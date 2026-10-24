using OrderProcessing.Shared.Helpers;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public sealed class InvoiceGenerator
    {
        private static string BuildInvoiceText(Order order, long checksum)
        {
            StringBuilder builder = new();

            builder.AppendLine($"Invoice for Order #{order.Id}");
            builder.AppendLine($"Customer ID: {order.CustomerId}");
            builder.AppendLine($"Created At: {order.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine("----------------------------------------");

            foreach (OrderItem item in order.Items)
            {
                builder.AppendLine(
                    $"Product #{item.ProductId} | " +
                    $"Qty: {item.Quantity} | " +
                    $"Unit Price: {item.UnitPrice.ToString("F2", CultureInfo.InvariantCulture)} | " +
                    $"Line Total: {(item.UnitPrice * item.Quantity).ToString("F2", CultureInfo.InvariantCulture)}");
            }

            builder.AppendLine("----------------------------------------");
            builder.AppendLine($"Subtotal: {order.SubTotal.ToString("F2", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"Discount: {order.Discount.ToString("F2", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"Shipping: {order.ShippingCost.ToString("F2", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"Total: {order.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"Rendering checksum: {checksum}");

            return builder.ToString();
        }

        public async Task GenerateAsync(Order order, CancellationToken ct)
        {
            const string source = "InvoiceGenerator";

            await SimulatedLatency.DelayAsync(source, "Preparing invoice...", 200, ct);

            Log.Info(source, $"Rendering invoice for order #{order.Id}...");

            long checksum = await Task.Run(() =>
            {
                long result = 0;

                for (int i = 0; i < 20_000_000; i++)
                {
                    if (i % 100_000 == 0)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    result += i % 97;
                }

                return result;
            }, ct);

            await SimulatedLatency.DelayAsync(source, "Preparing invoice file...", 100, ct);

            string invoicesDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "invoices");

            Directory.CreateDirectory(invoicesDirectory);

            string filePath = Path.Combine(invoicesDirectory, $"invoice_{order.Id}.txt");

            string tempPath = filePath + "." + Guid.NewGuid() + ".tmp";

            string invoiceText = BuildInvoiceText(order, checksum);

            try
            {
                await File.WriteAllTextAsync(
                    tempPath,
                    invoiceText,
                    Encoding.UTF8,
                    ct);

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

            Log.Success(source, $"Invoice generated: invoice_{order.Id}.txt");
        }
    }
}
