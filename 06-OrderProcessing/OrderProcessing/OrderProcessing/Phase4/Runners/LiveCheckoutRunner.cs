using OrderProcessing.Phase3.Services;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace OrderProcessing.Phase4.Runners
{
    public sealed class LiveCheckoutRunner
    {
        private readonly IncomingOrderRepository _incomingOrderRepository;

        public LiveCheckoutRunner(IncomingOrderRepository incomingOrderRepository)
        {
            _incomingOrderRepository = incomingOrderRepository;
        }

        public async Task RunAsync(Func<OrderProcessor> createProcessor, CancellationToken ct)
        {
            await DataSeeder.ResetAsync();

            List<Order> orders = await _incomingOrderRepository.GetAllAsync(ct);

            OrderProcessor processor = createProcessor();

            var batchTimer = Stopwatch.StartNew();

            var tasks = new List<Task<OrderRunResult>>(orders.Count);

            foreach (Order order in orders)
            {
                tasks.Add(ProcessAndMeasureAsync(processor, order, ct));
            }

            int completedCount = 0;

            await foreach (Task<OrderRunResult> completedTask in Task.WhenEach(tasks))
            {
                OrderRunResult result = await completedTask;

                completedCount++;

                string outcome = FormatOutcome(result.Order);

                string elapsed = result.Elapsed.TotalSeconds.ToString(
                    "F1",
                    CultureInfo.InvariantCulture);

                Console.WriteLine(
                    $"[{completedCount,3}/{orders.Count}] " +
                    $"Order #{result.Order.Id} -> {outcome} " +
                    $"after {elapsed} s");
            }

            batchTimer.Stop();

            PrintStatusCounts(orders);

            Console.WriteLine($"Batch finished in {batchTimer.Elapsed.TotalSeconds:F1} s");
        }

        private static async Task<OrderRunResult> ProcessAndMeasureAsync(
            OrderProcessor processor,
            Order order,
            CancellationToken ct)
        {
            var timer = Stopwatch.StartNew();

            await processor.ProcessOrderAsync(order, ct);

            timer.Stop();

            return new OrderRunResult(order, timer.Elapsed);
        }

        private static string FormatOutcome(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.FailureReason))
            {
                return order.Status.ToString();
            }

            return $"{order.Status} ({order.FailureReason})";
        }

        private static void PrintStatusCounts(List<Order> orders)
        {
            Console.WriteLine();
            Console.WriteLine("===== FINAL STATUS COUNTS =====");

            var counts = new Dictionary<OrderStatus, int>();

            foreach (Order order in orders)
            {
                if (!counts.ContainsKey(order.Status))
                {
                    counts[order.Status] = 0;
                }

                counts[order.Status]++;
            }

            foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
            {
                int count = counts.TryGetValue(status, out int value)
                    ? value
                    : 0;

                Console.WriteLine($"{status,-15}: {count}");
            }
        }

        private sealed record OrderRunResult(
            Order Order,
            TimeSpan Elapsed);
    }
}
