using OrderProcessing.Data;
using OrderProcessing.Helpers;
using OrderProcessing.Logger;
using OrderProcessing.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Repositories
{
    public sealed class OrderRepository
    {
        private readonly JsonFileStore _fileStore;

        public OrderRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public async Task SaveAsync(Order order, CancellationToken ct)
        {
            const string source = "OrderRepository";

            await SimulatedLatency.DelayAsync(source, "Opening connection...", 200, ct);

            await SimulatedLatency.DelayAsync(source, "Beginning transaction...", 100, ct);

            await SimulatedLatency.DelayAsync(source, "Inserting/updating order...", 400, ct);

            await _fileStore.UpdateAsync<Order, bool>(DataFiles.Orders,
                orders =>
                {
                    for (int i = 0; i < orders.Count; i++)
                    {
                        if (orders[i].Id == order.Id)
                        {
                            orders[i] = order;
                            return true;
                        }
                    }

                    orders.Add(order);
                    return true;
                },
                ct);

            await SimulatedLatency.DelayAsync(source, "Committing transaction...", 200, ct);

            Log.Success(source, $"Order {order.Id} saved");
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            const string source = "OrderRepository";

            await SimulatedLatency.DelayAsync(source, "Opening connection...", 200, ct);

            await SimulatedLatency.DelayAsync(source, "Executing query...", SimulatedLatency.GetQueryExecutionTime(), ct);

            List<Order> orders = await _fileStore.ReadAllAsync<Order>(DataFiles.Orders, ct);

            Log.Success(source, $"Retrieved {orders.Count} orders");

            return orders;
        }

        public async Task<List<Order>> GetByStatusAsync(OrderStatus s, CancellationToken ct)
        {
            const string source = "OrderRepository";

            await SimulatedLatency.DelayAsync(source, "Opening connection...", 200, ct);

            await SimulatedLatency.DelayAsync(source, "Executing query...", SimulatedLatency.GetQueryExecutionTime(), ct);

            List<Order> orders = await _fileStore.ReadAllAsync<Order>(DataFiles.Orders, ct);

            List<Order> matchingOrders = new();

            foreach (Order order in orders)
            {
                if (order.Status == s)
                {
                    matchingOrders.Add(order);
                }
            }

            Log.Success(source,$"Retrieved {matchingOrders.Count} orders with status {s}");

            return matchingOrders;
        }
    }
}
