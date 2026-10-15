using OrderProcessing.Logger;
using OrderProcessing.Models;
using OrderProcessing.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Services
{
    public sealed class OrderValidator
    {

        private readonly CustomerRepository _customerRepository;
        private readonly ProductRepository _productRepository;
        private readonly CouponRepository _couponRepository;
        private readonly OrderRepository _orderRepository;

        public OrderValidator(
            CustomerRepository customerRepository,
            ProductRepository productRepository,
            CouponRepository couponRepository,
            OrderRepository orderRepository)
        {
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _couponRepository = couponRepository;
            _orderRepository = orderRepository;
        }

        private async Task RejectOrderAsync(Order order, string reason, CancellationToken ct)
        {
            const string source = "OrderValidator";

            order.Status = OrderStatus.Rejected;
            order.FailureReason = reason;

            await _orderRepository.SaveAsync(order, ct);

            Log.Error(source, $"Order #{order.Id} rejected: {reason}");
        }

        public async Task ValidateOrderAsync(Order order, CancellationToken ct)
        {
            const string source = "OrderValidator";

            Log.Info(source, $"Validating order #{order.Id}...");

            order.Status = OrderStatus.Pending;
            order.FailureReason = null;

            // Customer Exists
            Customer? customer = await _customerRepository.GetByIdAsync(order.CustomerId, ct);

            if (customer is null)
            {
                await RejectOrderAsync(order,$"Customer #{order.CustomerId} not found", ct);

                return;
            }

            // Customer is active
            if (!customer.IsActive)
            {
                await RejectOrderAsync(order, "Customer is inactive", ct);

                return;
            }

            // Every product exists
            List<Product> products = new();

            foreach (OrderItem item in order.Items)
            {
                Product? product = await _productRepository.GetByIdAsync(item.ProductId, ct);

                if (product is null)
                {
                    await RejectOrderAsync(order, $"Product #{item.ProductId} not found", ct);

                    return;
                }

                products.Add(product);
            }

            // StockQuantity >= Quantity
            for (int i = 0; i < order.Items.Count; i++)
            {
                OrderItem item = order.Items[i];
                Product product = products[i];

                if (product.StockQuantity < item.Quantity)
                {
                    await RejectOrderAsync(order, $"Insufficient stock for '{product.Name}'", ct);

                    return;
                }
            }

            // Coupon exists
            Coupon? coupon = null;

            if (order.CouponCode is not null)
            {
                coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, ct);

                if (coupon is null)
                {
                    await RejectOrderAsync(order, $"Coupon '{order.CouponCode}' does not exist", ct);

                    return;
                }

                // Coupon has not expired
                if (coupon.ExpiryDate < DateTime.Today)
                {
                    await RejectOrderAsync(order, $"Coupon '{order.CouponCode}' has expired", ct);

                    return;
                }
            }

            order.Status = OrderStatus.Validated;
            order.FailureReason = null;

            await _orderRepository.SaveAsync(order, ct);

            Log.Success(source, $"Order #{order.Id} validated successfully");
        }

    }
}
