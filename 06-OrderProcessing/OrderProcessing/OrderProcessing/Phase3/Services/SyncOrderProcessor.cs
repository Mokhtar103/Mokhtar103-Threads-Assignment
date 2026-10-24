using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public sealed class SyncOrderProcessor
    {
        private readonly JsonFileStore _store;
        private readonly ConcurrentDictionary<int, byte> _usedThreads = new();

        public SyncOrderProcessor(JsonFileStore store)
        {
            _store = store;
        }

        public int UsedThreadCount => _usedThreads.Count;

        public bool ProcessOrder(Order order)
        {
            _usedThreads.TryAdd(Environment.CurrentManagedThreadId, 0);

            try
            {
                Delay("Customer query", 200);

                List<Customer> customers = _store.ReadAll<Customer>(DataFiles.Customers);

                Customer? customer = null;

                foreach (Customer item in customers)
                {
                    if (item.Id == order.CustomerId)
                    {
                        customer = item;
                        break;
                    }
                }

                if (customer is null)
                {
                    return Finish(order, OrderStatus.Rejected,$"Customer #{order.CustomerId} not found");
                }

                if (!customer.IsActive)
                {
                    return Finish(order, OrderStatus.Rejected,"Customer is inactive");
                }

                Delay("Inventory write", 300);

                bool reserved = _store.Update<Product, bool>(
                    DataFiles.Products,
                    products =>
                    {
                        foreach (OrderItem item in order.Items)
                        {
                            Product? product = FindProduct(products, item.ProductId);

                            if (product is null ||
                                product.StockQuantity < item.Quantity)
                            {
                                return false;
                            }
                        }

                        foreach (OrderItem item in order.Items)
                        {
                            Product product = FindProduct(products, item.ProductId)!;
                            product.StockQuantity -= item.Quantity;
                        }

                        return true;
                    });

                if (!reserved)
                {
                    string productName = FindInsufficientProductName(order);

                    return Finish(order, OrderStatus.Rejected,$"Insufficient stock for '{productName}'");
                }

                Delay("Pricing query", 200);

                List<Product> currentProducts = _store.ReadAll<Product>(DataFiles.Products);

                decimal subtotal = 0m;
                decimal totalWeight = 0m;
                int totalQuantity = 0;

                foreach (OrderItem item in order.Items)
                {
                    Product product = FindProduct(currentProducts, item.ProductId)!;

                    item.UnitPrice = product.Price;
                    subtotal += product.Price * item.Quantity;
                    totalWeight += product.WeightKg * item.Quantity;
                    totalQuantity += item.Quantity;
                }

                order.SubTotal = Money(subtotal);

                Delay("Discount API", 300);
                Delay("Shipping API", 300);

                int discountPercent = customer.LoyaltyTier switch
                {
                    LoyaltyTier.Gold => 10,
                    LoyaltyTier.Silver => 5,
                    _ => 0
                };

                if (!string.IsNullOrWhiteSpace(order.CouponCode))
                {
                    List<Coupon> coupons = _store.ReadAll<Coupon>(DataFiles.Coupons);

                    Coupon? coupon = null;

                    foreach (Coupon item in coupons)
                    {
                        if (string.Equals(
                            item.Code,
                            order.CouponCode,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            coupon = item;
                            break;
                        }
                    }

                    if (coupon is null ||
                        coupon.ExpiryDate < DateTime.Today)
                    {
                        ReleaseStock(order);

                        string reason = coupon is null
                            ? $"Coupon '{order.CouponCode}' does not exist"
                            : $"Coupon '{order.CouponCode}' has expired";

                        return Finish(order, OrderStatus.Rejected, reason);
                    }

                    discountPercent += coupon.DiscountPercent;
                }

                discountPercent = Math.Min(discountPercent, 30);

                order.Discount = Money(order.SubTotal * discountPercent / 100m);

                order.ShippingCost = order.SubTotal >= 1000m
                    ? 0m
                    : Money(5m + 1.5m * totalQuantity + 2m * totalWeight);

                order.TotalAmount = Money(
                    order.SubTotal - order.Discount + order.ShippingCost);

                Delay("Payment gateway", 700);

                string transactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

                order.TransactionId = transactionId;
                order.Status = OrderStatus.Completed;
                order.FailureReason = null;

                Delay("Order database save", 300);

                _store.Update<Order, bool>(
                    DataFiles.Orders,
                    orders =>
                    {
                        UpsertOrder(orders, order);
                        return true;
                    });

                Delay("Email provider", 300);

                var payment = new PaymentTransaction
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Status = PaymentStatus.Captured,
                    TransactionId = transactionId,
                    CreatedAt = DateTime.Now
                };

                _store.Update<PaymentTransaction, bool>(
                    DataFiles.Payments,
                    payments =>
                    {
                        payments.Add(payment);
                        return true;
                    });

                var email = new EmailMessage
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    To = customer.Email,
                    Subject = $"Order #{order.Id} confirmation",
                    Body = $"Your order #{order.Id} has been completed.",
                    SentAt = DateTime.Now,
                    Attempts = 1
                };

                _store.Update<EmailMessage, bool>(
                    DataFiles.Emails,
                    emails =>
                    {
                        emails.Add(email);
                        return true;
                    });

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Sync Order #{order.Id}", ex.Message);

                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;

                _store.Update<Order, bool>(
                    DataFiles.Orders,
                    orders =>
                    {
                        UpsertOrder(orders, order);
                        return true;
                    });

                return false;
            }
        }

        private bool Finish(
            Order order,
            OrderStatus status,
            string reason)
        {
            order.Status = status;
            order.FailureReason = reason;

            Delay("Order database save", 300);

            _store.Update<Order, bool>(
                DataFiles.Orders,
                orders =>
                {
                    UpsertOrder(orders, order);
                    return true;
                });

            return false;
        }

        private void ReleaseStock(Order order)
        {
            _store.Update<Product, bool>(
                DataFiles.Products,
                products =>
                {
                    foreach (OrderItem item in order.Items)
                    {
                        Product? product = FindProduct(products, item.ProductId);

                        if (product is not null)
                        {
                            product.StockQuantity += item.Quantity;
                        }
                    }

                    return true;
                });
        }

        private string FindInsufficientProductName(Order order)
        {
            List<Product> products = _store.ReadAll<Product>(DataFiles.Products);

            foreach (OrderItem item in order.Items)
            {
                Product? product = FindProduct(products, item.ProductId);

                if (product is null || product.StockQuantity < item.Quantity)
                {
                    return product?.Name ?? $"Product #{item.ProductId}";
                }
            }

            return "Unknown product";
        }

        private static Product? FindProduct(List<Product> products, int productId)
        {
            foreach (Product product in products)
            {
                if (product.Id == productId)
                {
                    return product;
                }
            }

            return null;
        }

        private static void UpsertOrder(List<Order> orders, Order order)
        {
            for (int i = 0; i < orders.Count; i++)
            {
                if (orders[i].Id == order.Id)
                {
                    orders[i] = order;
                    return;
                }
            }

            orders.Add(order);
        }

        private static decimal Money(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static void Delay(string step, int milliseconds)
        {
            Log.Info("SyncOrderProcessor",
                $"{step} - Thread.Sleep({milliseconds} ms)");

            Thread.Sleep(milliseconds);
        }
    }
}
