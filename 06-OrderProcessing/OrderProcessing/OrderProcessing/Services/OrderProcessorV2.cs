using OrderProcessing.Logger;
using OrderProcessing.Models;
using OrderProcessing.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Services
{
    public sealed class OrderProcessorV2
    {
        private readonly ProductRepository _productRepository;
        private readonly OrderRepository _orderRepository;
        private readonly OrderPricer _orderPricer;
        private readonly InvoiceGenerator _invoiceGenerator;

        private readonly ConcurrentDictionary<int, List<OrderItem>> _reservedItems = new();

        public OrderProcessorV2(
            ProductRepository productRepository,
            OrderRepository orderRepository,
            OrderPricer orderPricer,
            InvoiceGenerator invoiceGenerator)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderPricer = orderPricer;
            _invoiceGenerator = invoiceGenerator;
        }

        public async Task<bool> ProcessValidatedOrderAsync(Order order,CancellationToken ct)
        {
            const string source = "OrderProcessorV2";

            List<OrderItem> reserved = new();
            _reservedItems[order.Id] = reserved;

            foreach (OrderItem item in order.Items)
            {
                ct.ThrowIfCancellationRequested();

                Product? product =await _productRepository.GetByIdAsync(item.ProductId, ct);

                string productName = product?.Name ?? $"Product #{item.ProductId}";

                bool success =await _productRepository.ReserveStockAsync(item.ProductId, item.Quantity, ct);

                if (!success)
                {
                    await ReleaseReservedStockAsync(order, ct);

                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{productName}'";

                    await _orderRepository.SaveAsync(order, ct);

                    Log.Error(source, $"Order #{order.Id} rejected: {order.FailureReason}");

                    return false;
                }

                reserved.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            await _orderPricer.PriceOrderAsync(order, ct);

            await _invoiceGenerator.GenerateAsync(order, ct);

            order.Status = OrderStatus.Completed;
            order.FailureReason = null;

            await _orderRepository.SaveAsync(order, ct);

            _reservedItems.TryRemove(order.Id, out _);

            Log.Success(source, $"Order #{order.Id} completed successfully.");

            return true;
        }

        public async Task ReleaseReservedStockAsync(Order order, CancellationToken ct)
        {
            if (!_reservedItems.TryGetValue(order.Id, out List<OrderItem>? reserved))
            {
                return;
            }

            while (reserved.Count > 0)
            {
                OrderItem item = reserved[0];

                bool released =
                    await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, ct);

                if (!released)
                {
                    throw new InvalidOperationException($"Could not release stock for product #{item.ProductId}");
                }

                reserved.RemoveAt(0);
            }

            _reservedItems.TryRemove(order.Id, out _);
        }


    }
}
