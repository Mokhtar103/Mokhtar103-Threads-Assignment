using OrderProcessing.Helpers;
using OrderProcessing.Models;
using OrderProcessing.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Services
{
    public sealed class InventoryService
    {
        private readonly ProductRepository _productRepository;

        public InventoryService(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        private async Task ReleaseItemsAsync(List<OrderItem> items, CancellationToken ct)
        {
            foreach (OrderItem item in items)
            {
                await _productRepository.ReleaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    ct);
            }
        }

        public async Task<bool> ReserveStockAsync(Order order, CancellationToken ct)
        {
            var reservedItems = new List<OrderItem>();

            await SimulatedLatency.DelayAsync(
                "InventoryService",
                $"Starting reservation for order #{order.Id}...",
                200,
                ct);

            foreach (OrderItem item in order.Items)
            {
                bool reserved = await _productRepository.ReserveStockAsync(
                    item.ProductId,
                    item.Quantity,
                    ct);

                if (!reserved)
                {
                    await ReleaseItemsAsync(reservedItems, ct);
                    return false;
                }

                reservedItems.Add(item);
            }

            return true;
        }

        public async Task ReleaseStockAsync(Order order, CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync(
                "InventoryService",
                $"Releasing stock for order #{order.Id}...",
                200,
                ct);

            foreach (OrderItem item in order.Items)
            {
                await _productRepository.ReleaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    ct);
            }
        }

        
    }
}
