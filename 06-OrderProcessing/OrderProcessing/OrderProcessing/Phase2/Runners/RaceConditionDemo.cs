using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2.Runners
{
    public sealed class RaceConditionDemo
    {
        private readonly JsonFileStore _fileStore;
        private readonly ProductRepository _productRepository;

        public RaceConditionDemo(
            JsonFileStore fileStore,
            ProductRepository productRepository)
        {
            _fileStore = fileStore;
            _productRepository = productRepository;
        }

        private async Task SetProductStockAsync(int quantity, CancellationToken ct)
        {
            await _fileStore.UpdateAsync<Product, bool>(
                DataFiles.Products,
                products =>
                {
                    foreach (Product product in products)
                    {
                        if (product.Id == 4)
                        {
                            product.StockQuantity = quantity;
                            return true;
                        }
                    }

                    return false;
                },
                ct);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            const int productId = 4;

            Console.WriteLine();
            Console.WriteLine("===== Unsafe Reservation Demo =====");

            await SetProductStockAsync(1, ct);

            // Start both reservations 
            Task<bool> order10Unsafe = _productRepository.ReserveStockUnsafeAsync(productId, 1, ct);

            Task<bool> order11Unsafe = _productRepository.ReserveStockUnsafeAsync(productId, 1, ct);

            await Task.WhenAll(order10Unsafe, order11Unsafe);

            bool unsafeResult10 = await order10Unsafe;
            bool unsafeResult11 = await order11Unsafe;

            Product? unsafeKeyboard = await _productRepository.GetByIdAsync(productId, ct);

            Console.WriteLine(
                $"Order #10 reservation: {unsafeResult10}");

            Console.WriteLine($"Order #11 reservation: {unsafeResult11}");

            Console.WriteLine(
                $"Stock after unsafe reservations: " +
                $"{unsafeKeyboard?.StockQuantity}");

            Console.WriteLine();
            Console.WriteLine("===== Safe Reservation Demo =====");

            // Restore the original stock for another comparison
            await SetProductStockAsync(1, ct);

            Task<bool> order10Safe =_productRepository.ReserveStockAsync(productId, 1, ct);

            Task<bool> order11Safe =_productRepository.ReserveStockAsync(productId, 1, ct);

            await Task.WhenAll(order10Safe, order11Safe);

            bool safeResult10 = await order10Safe;
            bool safeResult11 = await order11Safe;

            Product? safeKeyboard = await _productRepository.GetByIdAsync(productId, ct);

            Console.WriteLine($"Order #10 reservation: {safeResult10}");

            Console.WriteLine($"Order #11 reservation: {safeResult11}");

            Console.WriteLine(
                $"Stock after safe reservations: " +
                $"{safeKeyboard?.StockQuantity}");
        }
    }
}
