using OrderProcessing.Data;
using OrderProcessing.Models;
using OrderProcessing.Repositories;
using OrderProcessing.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Runner
{
    public sealed class SyncAsyncComparisonRunner
    {
        private readonly JsonFileStore _store;
        private readonly IncomingOrderRepository _incomingOrderRepository;

        public SyncAsyncComparisonRunner(
            JsonFileStore store,
            IncomingOrderRepository incomingOrderRepository)
        {
            _store = store;
            _incomingOrderRepository = incomingOrderRepository;
        }

        public async Task RunAsync(Func<OrderProcessor> createAsyncProcessor, CancellationToken ct)
        {
            // Sync Version
            await DataSeeder.ResetAsync();

            List<Order> syncOrders = await _incomingOrderRepository.GetAllAsync(ct);

            syncOrders = syncOrders.GetRange(0, Math.Min(10, syncOrders.Count));

            var syncProcessor = new SyncOrderProcessor(_store);
            var syncThreads = new List<Thread>();

            int syncPoolBefore = ThreadPool.ThreadCount;
            var syncTimer = Stopwatch.StartNew();

            foreach (Order order in syncOrders)
            {
                Order capturedOrder = order;

                var thread = new Thread(() =>
                {
                    syncProcessor.ProcessOrder(capturedOrder);
                });

                syncThreads.Add(thread);
                thread.Start();
            }

            foreach (Thread thread in syncThreads)
            {
                thread.Join();
            }

            syncTimer.Stop();
            int syncPoolAfter = ThreadPool.ThreadCount;

            PrintResult(
                "SYNC",
                syncTimer.ElapsedMilliseconds,
                syncProcessor.UsedThreadCount,
                syncPoolBefore,
                syncPoolAfter);

            // Async Version
            await DataSeeder.ResetAsync();

            List<Order> asyncOrders = await _incomingOrderRepository.GetAllAsync(ct);

            asyncOrders = asyncOrders.GetRange(0, Math.Min(10, asyncOrders.Count));

            OrderProcessor asyncProcessor = createAsyncProcessor();

            int asyncPoolBefore = ThreadPool.ThreadCount;
            var asyncTimer = Stopwatch.StartNew();

            var tasks = new List<Task<bool>>();

            foreach (Order order in asyncOrders)
            {
                tasks.Add(asyncProcessor.ProcessOrderAsync(order, ct));
            }

            await Task.WhenAll(tasks);

            asyncTimer.Stop();
            int asyncPoolAfter = ThreadPool.ThreadCount;

            PrintResult(
                "ASYNC",
                asyncTimer.ElapsedMilliseconds,
                asyncProcessor.UsedThreadCount,
                asyncPoolBefore,
                asyncPoolAfter);
        }

        private static void PrintResult(
            string version,
            long elapsedMilliseconds,
            int usedThreads,
            int poolBefore,
            int poolAfter)
        {
            Console.WriteLine();
            Console.WriteLine($"===== {version} RESULTS =====");
            Console.WriteLine($"Elapsed: {elapsedMilliseconds} ms");
            Console.WriteLine($"Threads observed by processor: {usedThreads}");
            Console.WriteLine($"ThreadPool.ThreadCount before: {poolBefore}");
            Console.WriteLine($"ThreadPool.ThreadCount after:  {poolAfter}");
        }
    }
}
