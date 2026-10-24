using OrderProcessing.Phase1.Services;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase1.Runners
{
    public sealed class Phase1Runner
    {
        private readonly JsonFileStore _fileStore;
        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly OrderRepository _orderRepository;
        private readonly SyncOrderValidator _syncValidator;
        private readonly OrderValidator _asyncValidator;

        public Phase1Runner(
            JsonFileStore fileStore,
            IncomingOrderRepository incomingOrderRepository,
            OrderRepository orderRepository,
            SyncOrderValidator syncValidator,
            OrderValidator asyncValidator)
        {
            _fileStore = fileStore;
            _incomingOrderRepository = incomingOrderRepository;
            _orderRepository = orderRepository;
            _syncValidator = syncValidator;
            _asyncValidator = asyncValidator;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await DataSeeder.ResetAsync(ct);

            List<Order> incomingOrders = await _incomingOrderRepository.GetAllAsync(ct);

            Log.Info("Phase1Runner", "Starting sequential sync validation...");

            Stopwatch syncStopwatch = Stopwatch.StartNew();

            foreach (Order order in incomingOrders)
            {
                ct.ThrowIfCancellationRequested();
                _syncValidator.ValidateOrder(order);
            }

            syncStopwatch.Stop();

            double syncSeconds = syncStopwatch.Elapsed.TotalSeconds;

            Log.Success("Phase1Runner", $"Sync validation completed in {syncSeconds:F1} seconds");

            await DataSeeder.ResetAsync(ct);

            incomingOrders = await _incomingOrderRepository.GetAllAsync(ct);

            Log.Info("Phase1Runner", "Starting sequential async validation...");

            Stopwatch asyncStopwatch = Stopwatch.StartNew();

            foreach (Order order in incomingOrders)
            {
                ct.ThrowIfCancellationRequested();
                await _asyncValidator.ValidateOrderAsync(order, ct);
            }

            asyncStopwatch.Stop();

            double asyncSeconds = asyncStopwatch.Elapsed.TotalSeconds;

            List<Order> validatedOrders = await _orderRepository.GetByStatusAsync(OrderStatus.Validated, ct);

            List<Order> rejectedOrders = await _orderRepository.GetByStatusAsync(OrderStatus.Rejected, ct);

            List<Order> savedOrders = await _orderRepository.GetAllAsync(ct);

            Console.WriteLine();
            Console.WriteLine("===== Validation Summary =====");
            Console.WriteLine($"Total Orders: {savedOrders.Count}");
            Console.WriteLine($"Validated:    {validatedOrders.Count}");
            Console.WriteLine($"Rejected:     {rejectedOrders.Count}");

            foreach (Order order in rejectedOrders)
            {
                Console.WriteLine($"  - Order #{order.Id}: {order.FailureReason}");
            }

            Console.WriteLine($"Sync Time:  {syncSeconds:F1} seconds");
            Console.WriteLine($"Async Time: {asyncSeconds:F1} seconds");
            Console.WriteLine($"orders.json now contains {savedOrders.Count} records");
        }
    }
}
