using OrderProcessing.Phase1.Services;
using OrderProcessing.Phase2.Services;
using OrderProcessing.Phase3.Services;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase2.Runners
{
    public sealed class Phase2Runner
    {
        private readonly JsonFileStore _fileStore;
        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly OrderRepository _orderRepository;
        private readonly OrderValidator _orderValidator;
        private readonly RaceConditionDemo _raceConditionDemo;
        private readonly OrderPricer _orderPricer;
        private readonly OrderProcessorV2 _orderProcessor;
        private readonly ReportGenerator _reportGenerator;

        public Phase2Runner(
            JsonFileStore fileStore,
            IncomingOrderRepository incomingOrderRepository,
            OrderRepository orderRepository,
            OrderValidator orderValidator,
            RaceConditionDemo raceConditionDemo,
            OrderPricer orderPricer,
            OrderProcessorV2 orderProcessor,
            ReportGenerator reportGenerator)
        {
            _fileStore = fileStore;
            _incomingOrderRepository = incomingOrderRepository;
            _orderRepository = orderRepository;
            _orderValidator = orderValidator;
            _raceConditionDemo = raceConditionDemo;
            _orderPricer = orderPricer;
            _orderProcessor = orderProcessor;
            _reportGenerator = reportGenerator;
        }

        private static Order CloneOrder(Order source)
        {
            Order copy = new()
            {
                Id = source.Id,
                CustomerId = source.CustomerId,
                CouponCode = source.CouponCode,
                SubTotal = 0m,
                Discount = 0m,
                ShippingCost = 0m,
                TotalAmount = 0m,
                Status = source.Status,
                CreatedAt = source.CreatedAt,
                FailureReason = source.FailureReason,
                TransactionId = source.TransactionId
            };

            foreach (OrderItem item in source.Items)
            {
                copy.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = 0m
                });
            }

            return copy;
        }

        public async Task RunAsync(CancellationToken ct)
        {

            await DataSeeder.ResetAsync(ct);

            await _raceConditionDemo.RunAsync(ct);

            await DataSeeder.ResetAsync(ct);

            List<Order> incoming = await _incomingOrderRepository.GetAllAsync(ct);

            Order? firstOrder = null;

            foreach (Order order in incoming)
            {
                if (order.Id == 1)
                {
                    firstOrder = order;
                    break;
                }
            }

            if (firstOrder is null)
            {
                throw new InvalidOperationException("Order #1 was not found in incoming_orders.json.");
            }

            Order sequentialOrder = CloneOrder(firstOrder);
            Order concurrentOrder = CloneOrder(firstOrder);

            Stopwatch sequentialWatch = Stopwatch.StartNew();

            await _orderPricer.PriceOrderSequentialAsync(sequentialOrder, ct);

            sequentialWatch.Stop();

            Stopwatch concurrentWatch = Stopwatch.StartNew();

            await _orderPricer.PriceOrderAsync(concurrentOrder, ct);

            concurrentWatch.Stop();

            Console.WriteLine();
            Console.WriteLine("===== Pricing Comparison: Order #1 =====");
            Console.WriteLine($"Sequential: {sequentialWatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"Concurrent: {concurrentWatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"Subtotal: {concurrentOrder.SubTotal:F2}");
            Console.WriteLine($"Discount: {concurrentOrder.Discount:F2}");
            Console.WriteLine($"Shipping: {concurrentOrder.ShippingCost:F2}");
            Console.WriteLine($"Total: {concurrentOrder.TotalAmount:F2}");

            Stopwatch phaseWatch = Stopwatch.StartNew();

            List<Task> validationTasks = new();

            foreach (Order order in incoming)
            {
                validationTasks.Add(
                    _orderValidator.ValidateOrderAsync(order, ct));
            }

            await Task.WhenAll(validationTasks);

            List<Order> savedOrders = await _orderRepository.GetAllAsync(ct);

            List<Order> validatedOrders = new();

            foreach (Order order in savedOrders)
            {
                if (order.Status == OrderStatus.Validated)
                {
                    validatedOrders.Add(order);
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                $"Validated concurrently: {validatedOrders.Count} orders");

            List<Task<bool>> processingTasks = new();
            List<Order> processingOrders = new();

            foreach (Order order in validatedOrders)
            {
                processingOrders.Add(order);

                processingTasks.Add(
                    _orderProcessor.ProcessValidatedOrderAsync(order, ct));
            }

            if (processingTasks.Count > 0)
            {
                Console.WriteLine(
                    $"First task status immediately: {processingTasks[0].Status}");
            }

            await Task.Delay(1000, ct);

            Console.WriteLine();
            Console.WriteLine("===== Task Status After 1 Second =====");

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine(
                    $"Order #{processingOrders[i].Id}: " +
                    $"{processingTasks[i].Status}");
            }

            int finishedCount = 0;
            int cancelledCount = 0;

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Task<bool> task = processingTasks[i];
                Order order = processingOrders[i];

                try
                {
                    await task;
                    finishedCount++;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine(
                        $"Order #{order.Id} task status: {task.Status}");

                    await _orderProcessor.ReleaseReservedStockAsync(
                        order, CancellationToken.None);

                    order.Status = OrderStatus.Pending;
                    order.FailureReason = "Cancelled";

                    await _orderRepository.SaveAsync(
                        order, CancellationToken.None);

                    cancelledCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Order #{order.Id} task status: {task.Status}");

                    await _orderProcessor.ReleaseReservedStockAsync(order, ct);

                    order.Status = OrderStatus.Failed;
                    order.FailureReason = ex.Message;

                    await _orderRepository.SaveAsync(order, ct);

                    Log.Error(
                        "Phase2Runner",
                        $"Order #{order.Id} failed: {ex.Message}");

                    finishedCount++;
                }
            }
            phaseWatch.Stop();

            Console.WriteLine();
            Console.WriteLine("===== Final Task Statuses =====");

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine(
                    $"Order #{processingOrders[i].Id}: " +
                    $"{processingTasks[i].Status}");
            }

            List<Order> finalOrders = await _orderRepository.GetAllAsync(ct);

            int completed = 0;
            int rejected = 0;
            int failed = 0;

            foreach (Order order in finalOrders)
            {
                switch (order.Status)
                {
                    case OrderStatus.Completed:
                        completed++;
                        break;

                    case OrderStatus.Rejected:
                        rejected++;
                        break;

                    case OrderStatus.Failed:
                        failed++;
                        break;
                }
            }

            Console.WriteLine();
            Console.WriteLine("===== Phase 2 Summary =====");
            Console.WriteLine($"Total orders in orders.json: {finalOrders.Count}");
            Console.WriteLine($"Completed: {completed}");
            Console.WriteLine($"Rejected:  {rejected}");
            Console.WriteLine($"Failed:    {failed}");
            Console.WriteLine(
                $"Processing time: {phaseWatch.Elapsed.TotalSeconds:F2} seconds");

            if (finalOrders.Count != 20)
            {
                throw new InvalidOperationException($"Expected 20 saved orders, found {finalOrders.Count}");
            }

            Console.WriteLine("Verified: orders.json contains exactly 20 records");

            Console.WriteLine();

            Task<Report> reportTask = _reportGenerator.GenerateAsync(ct);

            Console.WriteLine("Generating report...");

            Report report = await reportTask;

            Console.WriteLine();
            Console.WriteLine("===== Phase 2 Report =====");
            Console.WriteLine($"Generated At:       {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Total Orders:       {report.TotalOrders}");
            Console.WriteLine($"Completed:          {report.CompletedCount}");
            Console.WriteLine($"Rejected:           {report.RejectedCount}");
            Console.WriteLine($"Failed:             {report.FailedCount}");
            Console.WriteLine($"Payment Failed:     {report.PaymentFailedCount}");
            Console.WriteLine($"Total Revenue:      {report.TotalRevenue:F2}");
            Console.WriteLine($"Average Order Value:{report.AverageOrderValue,10:F2}");
            Console.WriteLine($"Top Selling Product: {report.TopSellingProduct}");
            Console.WriteLine($"Top Selling Quantity: {report.TopSellingQuantity}");

            if (cancelledCount > 0)
            {
                Console.WriteLine(
                    $"Run cancelled — {finishedCount} orders finished, " +
                    $"{cancelledCount} cancelled");
            }
        }
    }
}
