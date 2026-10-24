using OrderProcessing.Shared.Helpers;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Repositories
{
    public sealed class IncomingOrderRepository
    {
        private readonly JsonFileStore _fileStore;

        public IncomingOrderRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            const string source = "IncomingOrderRepository";

            await SimulatedLatency.DelayAsync(source, "Opening connection...", 200, ct);

            await SimulatedLatency.DelayAsync(source, "Executing query...", 400, ct);

            List<Order> orders =await _fileStore.ReadAllAsync<Order>(DataFiles.IncomingOrders, ct);

            Log.Success(source, $"Retrieved {orders.Count} incoming orders");

            return orders;
        }
    }
}
