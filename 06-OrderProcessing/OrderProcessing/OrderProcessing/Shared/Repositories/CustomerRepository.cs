using OrderProcessing.Shared.Helpers;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Repositories
{
    public sealed class CustomerRepository
    {
        private readonly JsonFileStore _fileStore;

        public CustomerRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
        {
            const string source = "CustomerRepository";

            await SimulatedLatency.DelayAsync(source, "Opening connection...", 200, ct);

            await SimulatedLatency.DelayAsync(source, "Executing query...", 400, ct);

            List<Customer> customers =await _fileStore.ReadAllAsync<Customer>(DataFiles.Customers, ct);

            Customer? customer = null;

            foreach (Customer item in customers)
            {
                if (item.Id == id)
                {
                    customer = item;
                    break;
                }
            }

            if (customer is null)
            {
                Log.Error(source, $"Customer {id} not found");
            }
            else
            {
                Log.Success(source, $"Customer {id} found");
            }

            return customer;
        }
    }
}
