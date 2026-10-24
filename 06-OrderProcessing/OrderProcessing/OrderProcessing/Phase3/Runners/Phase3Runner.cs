using OrderProcessing.Phase3.Services;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Runners
{
    public sealed class Phase3Runner
    {
        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly OrderRepository _orderRepository;
        private readonly OrderProcessor _orderProcessor;

        public Phase3Runner(
            IncomingOrderRepository incomingOrderRepository,
            OrderRepository orderRepository,
            OrderProcessor orderProcessor)
        {
            _incomingOrderRepository = incomingOrderRepository;
            _orderRepository = orderRepository;
            _orderProcessor = orderProcessor;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await DataSeeder.ResetAsync();

            List<Order> incomingOrders = await _incomingOrderRepository.GetAllAsync(ct);

            var tasks = new List<Task<bool>>();

            foreach (Order order in incomingOrders)
            {
                tasks.Add(_orderProcessor.ProcessOrderAsync(order, ct));
            }

            foreach (Task<bool> task in tasks)
            {
                await task;
            }

            List<Order> finalOrders = await _orderRepository.GetAllAsync(CancellationToken.None);

            Console.WriteLine();
            Console.WriteLine(
                $"{"Order",-8} {"Status",-16} {"Total",-12} Reason");
            Console.WriteLine(new string('-', 90));

            foreach (Order order in finalOrders)
            {
                Console.WriteLine(
                    $"{order.Id,-8} " +
                    $"{order.Status,-16} " +
                    $"{order.TotalAmount,-12:F2} " +
                    $"{order.FailureReason ?? "-"}");
            }

            Console.WriteLine(new string('-', 90));

            PrintFinalCounts(finalOrders);
        }

        private static void PrintFinalCounts(List<Order> orders)
        {
            int completed = 0;
            int rejected = 0;
            int paymentFailed = 0;
            int failed = 0;

            foreach (Order order in orders)
            {
                switch (order.Status)
                {
                    case OrderStatus.Completed:
                        completed++;
                        break;

                    case OrderStatus.Rejected:
                        rejected++;
                        break;

                    case OrderStatus.PaymentFailed:
                        paymentFailed++;
                        break;

                    case OrderStatus.Failed:
                        failed++;
                        break;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Total orders: {orders.Count}");
            Console.WriteLine($"Completed: {completed}");
            Console.WriteLine($"Rejected: {rejected}");
            Console.WriteLine($"PaymentFailed: {paymentFailed}");
            Console.WriteLine($"Failed: {failed}");
        }
    }
}
