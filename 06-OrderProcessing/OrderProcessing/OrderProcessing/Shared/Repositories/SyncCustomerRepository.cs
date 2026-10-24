using OrderProcessing.Shared.Helpers;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Repositories
{
    public sealed class SyncCustomerRepository
    {
        private readonly JsonFileStore _fileStore;

        public SyncCustomerRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public Customer? GetById(int id)
        {
            const string source = "SyncCustomerRepository";

            SyncSimulatedLatency.Delay(source, "Opening connection...", 200);

            SyncSimulatedLatency.Delay(source, "Executing query...", 400);

            List<Customer> customers = _fileStore.ReadAll<Customer>(DataFiles.Customers);

            foreach (Customer customer in customers)
            {
                if (customer.Id == id)
                {
                    Log.Success(source, $"Customer {id} found");
                    return customer;
                }
            }

            Log.Info(source, $"Customer {id} not found");
            return null;
        }
    }
}
