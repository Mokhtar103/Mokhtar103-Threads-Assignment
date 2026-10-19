using OrderProcessing.Data;
using OrderProcessing.Helpers;
using OrderProcessing.Logger;
using OrderProcessing.Models;
using OrderProcessing.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OrderProcessing.Services
{
    public sealed class ReportGenerator
    {
        private readonly OrderRepository _orderRepository;
        private readonly ProductRepository _productRepository;

        public ReportGenerator(OrderRepository orderRepository, ProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        // helper method for rounding

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static async Task SaveReportAsync(Report report, CancellationToken ct)
        {
            string reportsDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                DataFiles.ReportsDirectory);

            Directory.CreateDirectory(reportsDirectory);

            string fileName = $"report_{report.GeneratedAt:yyyyMMdd_HHmmss}.json";

            string filePath = Path.Combine(reportsDirectory, fileName);
            string tempPath = filePath + "." + Guid.NewGuid() + ".tmp";

            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(report, JsonFileStore.JsonOptions);

            try
            {
                await using (FileStream stream = new(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    options: FileOptions.Asynchronous))
                {
                    await stream.WriteAsync(jsonBytes, ct);
                    await stream.FlushAsync(ct);
                    stream.Flush(flushToDisk: true);
                }

                ct.ThrowIfCancellationRequested();

                File.Move(tempPath, filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public async Task<Report> GenerateAsync(CancellationToken ct)
        {
            const string source = "ReportGenerator";

            await SimulatedLatency.DelayAsync(source, "Preparing report...", 200, ct);

            List<Order> orders = await _orderRepository.GetAllAsync(ct);

            await SimulatedLatency.DelayAsync(source, "Aggregating order statistics...", 400, ct);

            Report report = new()
            {
                TotalOrders = orders.Count,
                GeneratedAt = DateTime.Now
            };

            decimal totalRevenue = 0m;
            int completedCount = 0;

            Dictionary<int, int> soldQuantities = new();

            foreach (Order order in orders)
            {
                switch (order.Status)
                {
                    case OrderStatus.Completed:
                        report.CompletedCount++;
                        completedCount++;
                        totalRevenue += order.TotalAmount;

                        foreach (OrderItem item in order.Items)
                        {
                            if (soldQuantities.ContainsKey(item.ProductId))
                            {
                                soldQuantities[item.ProductId] += item.Quantity;
                            }
                            else
                            {
                                soldQuantities[item.ProductId] = item.Quantity;
                            }
                        }

                        break;

                    case OrderStatus.Rejected:
                        report.RejectedCount++;
                        break;

                    case OrderStatus.Failed:
                        report.FailedCount++;
                        break;

                    case OrderStatus.PaymentFailed:
                        report.PaymentFailedCount++;
                        break;
                }
            }

            report.TotalRevenue = RoundMoney(totalRevenue);

            report.AverageOrderValue = completedCount == 0
                ? 0m
                : RoundMoney(totalRevenue / completedCount);

            int topProductId = 0;
            int topQuantity = 0;

            foreach (KeyValuePair<int, int> entry in soldQuantities)
            {
                if (entry.Value > topQuantity ||
                    (entry.Value == topQuantity &&
                     entry.Value > 0 &&
                     (topProductId == 0 || entry.Key < topProductId)))
                {
                    topProductId = entry.Key;
                    topQuantity = entry.Value;
                }
            }

            if (topProductId != 0)
            {
                Product? topProduct =
                    await _productRepository.GetByIdAsync(topProductId, ct);

                report.TopSellingProduct = topProduct?.Name ?? $"Product #{topProductId}";

                report.TopSellingQuantity = topQuantity;
            }

            await SaveReportAsync(report, ct);

            Log.Success(source, $"Report saved. Total orders: {report.TotalOrders}");

            return report;
        }
    }
}
