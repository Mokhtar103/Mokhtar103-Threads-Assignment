using OrderProcessing.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase4.Runners
{
    public sealed class FraudScoringRunner
    {
        private const int OrderCount = 10_000;
        private const int FraudThreshold = 90;

        private static int Score(int orderId)
        {
            long h = orderId;

            for (int i = 0; i < 100_000; i++)
            {
                h = (h * 31 + i) % 1_000_000_007;
            }

            return (int)(h % 100);
        }

        public void Run(CancellationToken ct)
        {
            var orders = new List<Order>(OrderCount);

            for (int id = 1; id <= OrderCount; id++)
            {
                orders.Add(new Order { Id = id });
            }

            var sequentialFlagged = new List<int>();
            var sequentialTimer = Stopwatch.StartNew();

            foreach (Order order in orders)
            {
                ct.ThrowIfCancellationRequested();

                if (Score(order.Id) >= FraudThreshold)
                {
                    sequentialFlagged.Add(order.Id);
                }
            }

            sequentialTimer.Stop();

            var parallelFlagged = new ConcurrentBag<int>();
            var threadIds = new ConcurrentDictionary<int, byte>();

            var parallelTimer = Stopwatch.StartNew();

            Parallel.ForEach(
                orders,
                new ParallelOptions
                {
                    CancellationToken = ct,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                },
                order =>
                {
                    threadIds.TryAdd(
                        Environment.CurrentManagedThreadId, 0);

                    ct.ThrowIfCancellationRequested();

                    if (Score(order.Id) >= FraudThreshold)
                    {
                        parallelFlagged.Add(order.Id);
                    }
                });

            parallelTimer.Stop();

            sequentialFlagged.Sort();

            List<int> parallelIds = parallelFlagged.ToList();

            parallelIds.Sort();

            bool sameResults = sequentialFlagged.SequenceEqual(parallelIds);

            bool noDuplicates = parallelIds.Distinct().Count() == parallelIds.Count;

            if (!sameResults || !noDuplicates)
            {
                throw new InvalidOperationException("Sequential and parallel fraud results do not match.");
            }

            if (sequentialFlagged.Count != 1005 ||
                !sequentialFlagged.Take(5)
                    .SequenceEqual(new[] { 4, 8, 12, 29, 33 }))
            {
                throw new InvalidOperationException("Fraud scoring results do not match the expected results.");
            }

            double speedUp =
                parallelTimer.Elapsed.TotalMilliseconds == 0
                    ? 0
                    : sequentialTimer.Elapsed.TotalMilliseconds /
                      parallelTimer.Elapsed.TotalMilliseconds;

            Console.WriteLine("\n===== TASK 10.4 A: Fraud Scoring =====");
            Console.WriteLine($"Sequential flagged: {sequentialFlagged.Count}");
            Console.WriteLine($"Parallel flagged:   {parallelIds.Count}");
            Console.WriteLine($"First five IDs:     {string.Join(", ", parallelIds.Take(5))}");
            Console.WriteLine($"Sequential time:    {sequentialTimer.Elapsed.TotalMilliseconds:F0} ms");
            Console.WriteLine($"Parallel time:      {parallelTimer.Elapsed.TotalMilliseconds:F0} ms");
            Console.WriteLine($"Speed-up:           {speedUp:F2}x");
            Console.WriteLine($"Distinct threads:   {threadIds.Count}");
            Console.WriteLine($"Processor count:    {Environment.ProcessorCount}");
            Console.WriteLine($"Results identical:  {sameResults}");
            Console.WriteLine($"No duplicate IDs:   {noDuplicates}");
        }
    }
}
