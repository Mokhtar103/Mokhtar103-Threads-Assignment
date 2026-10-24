using OrderProcessing.Shared.Helpers;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Repositories
{
    public sealed class SyncProductRepository
    {
        private readonly JsonFileStore _fileStore;

        public SyncProductRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public Product? GetById(int id)
        {
            const string source = "SyncProductRepository";

            SyncSimulatedLatency.Delay(source, "Opening connection...", 200);

            SyncSimulatedLatency.Delay(source, "Executing query...", 400);

            List<Product> products = _fileStore.ReadAll<Product>(DataFiles.Products);

            foreach (Product product in products)
            {
                if (product.Id == id)
                {
                    Log.Success(source, $"Product {id} found");
                    return product;
                }
            }

            Log.Info(source, $"Product {id} not found");
            return null;
        }
    }
}
