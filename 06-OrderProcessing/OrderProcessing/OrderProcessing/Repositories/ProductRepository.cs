using OrderProcessing.Data;
using OrderProcessing.Helpers;
using OrderProcessing.Logger;
using OrderProcessing.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Repositories
{
    public sealed class ProductRepository
    {
        private readonly JsonFileStore _fileStore;

        public ProductRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
        {
            const string source = "ProductRepository";

            await SimulatedLatency.DelayAsync(source, "Opening connection...", 200, ct);

            await SimulatedLatency.DelayAsync(source,"Executing query...",SimulatedLatency.GetQueryExecutionTime(), ct);

            List<Product> products =await _fileStore.ReadAllAsync<Product>(DataFiles.Products, ct);

            Product? product = null;

            foreach (Product item in products)
            {
                if (item.Id == id)
                {
                    product = item;
                    break;
                }
            }

            if (product is null)
            {
                Log.Error(source, $"Product {id} not found");
            }
            else
            {
                Log.Success(source, $"Product {id} found");
            }

            return product;
        }

        public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken ct)
        {

            const string source = "ProductRepository";

            if (qty <= 0)
            {
                Log.Error(source, "Reservation quantity must be positive");
                return false;
            }

            await SimulatedLatency.DelayAsync(source, "Opening connection...", 200, ct);

            await SimulatedLatency.DelayAsync(source, "Beginning transaction...", 100, ct);

            await SimulatedLatency.DelayAsync(source, "Updating product stock...", 400, ct);


            bool reserved = await _fileStore.UpdateAsync<Product, bool>(DataFiles.Products,
                products =>
                {
                    foreach (Product product in products)
                    {
                        if (product.Id != productId)
                        {
                            continue;
                        }

                        if (product.StockQuantity < qty)
                        {
                            return false;
                        }

                        product.StockQuantity -= qty;
                        return true;
                    }

                    return false;
                }, ct);

            await SimulatedLatency.DelayAsync(source, "Committing transaction...", 200, ct);

            if (reserved)
            {
                Log.Success(source, $"Reserved {qty} units of product {productId}");
            }
            else
            {
                Log.Error(source, $"Could not reserve {qty} units of product {productId}");
            }

            return reserved;
        }
     }
}
