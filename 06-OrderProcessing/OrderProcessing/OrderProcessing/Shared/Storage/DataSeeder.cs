using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Storage
{
    public class DataSeeder
    {
        private static readonly string DataDirectory =Path.Combine(AppContext.BaseDirectory, "Data");

        public static async Task ResetAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (Directory.Exists(DataDirectory))
            {
                Directory.Delete(DataDirectory, recursive: true);
            }

            Directory.CreateDirectory(DataDirectory);

            Directory.CreateDirectory(Path.Combine(DataDirectory, DataFiles.InvoicesDirectory));

            Directory.CreateDirectory(Path.Combine(DataDirectory, DataFiles.ReportsDirectory));

            var store = new JsonFileStore(DataDirectory);


            Customer[] customers =
            [
                new() { Id = 1, Name = "Ahmed Ali", Email = "ahmed.ali@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
                new() { Id = 2, Name = "Sara Mohamed", Email = "sara.mohamed@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
                new() { Id = 3, Name = "Omar Hassan", Email = "omar.hassan@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
                new() { Id = 4, Name = "Mona Ibrahim", Email = "mona.ibrahim@example.com", IsActive = false, LoyaltyTier = LoyaltyTier.Silver },
                new() { Id = 5, Name = "Youssef Adel", Email = "youssef.adel@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
                new() { Id = 6, Name = "Nour Khaled", Email = "nour.khaled@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
                new() { Id = 7, Name = "Karim Samir", Email = "karim.samir@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
                new() { Id = 8, Name = "Laila Mahmoud", Email = "laila.mahmoud@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
                new() { Id = 9, Name = "Hassan Tarek", Email = "hassan.tarek@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
                new() { Id = 10, Name = "Dina Fathy", Email = "dina.fathy@example.com", IsActive = true, LoyaltyTier = LoyaltyTier.Silver }
            ];

           

            Product[] products =
            [
                new() { Id = 1, Name = "Laptop", Price = 1200.00m, StockQuantity = 10, WeightKg = 2.2m },
                new() { Id = 2, Name = "Smartphone", Price = 800.00m, StockQuantity = 15, WeightKg = 0.4m },
                new() { Id = 3, Name = "Wireless Mouse", Price = 25.00m, StockQuantity = 2, WeightKg = 0.1m },
                new() { Id = 4, Name = "Mechanical Keyboard", Price = 90.00m, StockQuantity = 1, WeightKg = 0.9m },
                new() { Id = 5, Name = "USB-C Hub", Price = 45.00m, StockQuantity = 30, WeightKg = 0.2m },
                new() { Id = 6, Name = "Headphones", Price = 150.00m, StockQuantity = 12, WeightKg = 0.3m },
                new() { Id = 7, Name = "27\" Monitor", Price = 350.00m, StockQuantity = 6, WeightKg = 5.0m },
                new() { Id = 8, Name = "Webcam", Price = 70.00m, StockQuantity = 20, WeightKg = 0.2m }
            ];


            DateTime today = DateTime.Today;

            Coupon[] coupons =
            [
                new() { Code = "SAVE10", DiscountPercent = 10, ExpiryDate = today.AddDays(180) },
                new() { Code = "WELCOME15", DiscountPercent = 15, ExpiryDate = today.AddDays(90) },
                new() { Code = "FLASH20", DiscountPercent = 20, ExpiryDate = today.AddDays(7) },
                new() { Code = "OLD20", DiscountPercent = 20, ExpiryDate = today.AddDays(-30) }
            ];

            DateTime seededAt = DateTime.Now;


            Order[] incomingOrders =
            [
                CreateOrder(1, 1, seededAt, "SAVE10",
                (1, 1), (3, 1)),

                CreateOrder(2, 2, seededAt, null,
                (2, 1)),

                CreateOrder(3, 3, seededAt, "WELCOME15",
                    (5, 2), (8, 1)),

                CreateOrder(4, 4, seededAt, null,
                    (6, 1)),

                CreateOrder(5, 5, seededAt, null,
                    (7, 1)),

                CreateOrder(6, 6, seededAt, "FLASH20",
                    (5, 1)),

                CreateOrder(7, 7, seededAt, null,
                    (6, 1), (8, 1)),

                CreateOrder(8, 8, seededAt, "SAVE10",
                    (1, 1)),

                CreateOrder(9, 9, seededAt, null,
                    (3, 5)),

                CreateOrder(10, 10, seededAt, null,
                    (4, 1)),

                CreateOrder(11, 1, seededAt, null,
                    (4, 1)),

                CreateOrder(12, 2, seededAt, "OLD20",
                    (2, 1)),

                CreateOrder(13, 3, seededAt, null,
                    (6, 2)),

                CreateOrder(14, 5, seededAt, "WELCOME15",
                    (8, 2), (5, 1)),

                CreateOrder(15, 99, seededAt, null,
                    (2, 1)),

                CreateOrder(16, 6, seededAt, null,
                    (7, 1), (5, 1)),

                CreateOrder(17, 7, seededAt, "SAVE10",
                    (2, 1)),

                CreateOrder(18, 8, seededAt, "FAKE50",
                    (6, 1)),

                CreateOrder(19, 9, seededAt, null,
                    (8, 1)),

                CreateOrder(20, 10, seededAt, "FLASH20",
                    (1, 1), (6, 1))
            ];

            // ---------------- EMPTY OUTPUT COLLECTIONS ----------------

            Order[] orders = [];
            PaymentTransaction[] payments = [];
            EmailMessage[] emails = [];
            EmailMessage[] emailRetry = [];

            await WriteSeedAsync(DataFiles.Customers, customers);
            await WriteSeedAsync(DataFiles.Products, products);
            await WriteSeedAsync(DataFiles.Coupons, coupons);
            await WriteSeedAsync(DataFiles.IncomingOrders, incomingOrders);
            await WriteSeedAsync(DataFiles.Orders, orders);
            await WriteSeedAsync(DataFiles.Payments, payments);
            await WriteSeedAsync(DataFiles.Emails, emails);
            await WriteSeedAsync(DataFiles.EmailRetry, emailRetry);

            async Task WriteSeedAsync<T>(string fileName, IEnumerable<T> records)
            {
                ct.ThrowIfCancellationRequested();

                List<T> collection = new();

                foreach (T record in records)
                {
                    collection.Add(record);
                }

                await store.WriteAllAsync(fileName, collection, ct);

                Console.WriteLine($"[Seeder] {fileName} -> {collection.Count} records");
            }
        }

        private static Order CreateOrder(
            int orderId,
            int customerId,
            DateTime createdAt,
            string? couponCode,
            params (int ProductId, int Quantity)[] items)
        {
            List<OrderItem> orderItems = new();

            foreach (var item in items)
            {
                OrderItem orderItem = new()
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = 0m
                };

                orderItems.Add(orderItem);
            }

            return new Order
            {
                Id = orderId,
                CustomerId = customerId,
                Items = orderItems,
                CouponCode = couponCode,
                SubTotal = 0m,
                Discount = 0m,
                ShippingCost = 0m,
                TotalAmount = 0m,
                Status = OrderStatus.Pending,
                CreatedAt = createdAt,
                FailureReason = null,
                TransactionId = null
            };
        }
    }
}
