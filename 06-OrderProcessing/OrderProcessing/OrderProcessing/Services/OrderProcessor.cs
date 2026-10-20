using OrderProcessing.Data;
using OrderProcessing.Exceptions;
using OrderProcessing.Logger;
using OrderProcessing.Models;
using OrderProcessing.Repositories;
using OrderProcessing.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Services
{
    public sealed class OrderProcessor
    {
        private readonly CustomerService _customerService;
        private readonly InventoryService _inventoryService;
        private readonly PricingService _pricingService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly OrderRepository _orderRepository;
        private readonly IEmailService _emailService;
        private readonly ProductRepository _productRepository;
        private readonly JsonFileStore _fileStore;

        private readonly ConcurrentDictionary<int, byte> _usedThreads = new();

        public int UsedThreadCount => _usedThreads.Count;

        public OrderProcessor(
            CustomerService customerService,
            InventoryService inventoryService,
            PricingService pricingService,
            IPaymentGateway paymentGateway,
            OrderRepository orderRepository,
            IEmailService emailService,
            ProductRepository productRepository,
            JsonFileStore fileStore)
        {
            _customerService = customerService;
            _inventoryService = inventoryService;
            _pricingService = pricingService;
            _paymentGateway = paymentGateway;
            _orderRepository = orderRepository;
            _emailService = emailService;
            _productRepository = productRepository;
            _fileStore = fileStore;
        }

        //#pragma warning disable CS4014
        //        public async Task ProcessOrderAsync(Order order, Customer customer, CancellationToken ct)
        //        {
        //            await _orderRepository.SaveAsync(order, ct);
        //            _emailService.SendConfirmationAsync(order, customer, ct); // not awaited
        //            Console.WriteLine("Order processing finished");
        //        }
        //#pragma warning restore CS4014

        public async Task<bool> ProcessOrderAsync(Order order, CancellationToken ct)
        {
            Customer? customer = null;
            bool stockReserved = false;
            string? transactionId = null;

            try
            {
                var timer = Stopwatch.StartNew();

                try
                {
                    customer = await _customerService.GetCustomerAsync(order.CustomerId, ct);
                }
                catch (CustomerNotFoundException ex)
                {
                    timer.Stop();
                    LogStep(order, 1, $"Customer ... REJECTED ({ex.Message})", timer.ElapsedMilliseconds);

                    return await FinishEarlyAsync(order, OrderStatus.Rejected, ex.Message, ct);
                }

                if (!customer.IsActive)
                {
                    timer.Stop();
                    const string reason = "Customer is inactive";

                    LogStep(order, 1, $"Customer ... REJECTED ({reason})", timer.ElapsedMilliseconds);

                    return await FinishEarlyAsync(order, OrderStatus.Rejected, reason, ct);
                }

                timer.Stop();
                LogStep(order, 1, "Customer ... OK", timer.ElapsedMilliseconds);

                // Step 2/6: Reserve stock
                timer.Restart();

                bool reserved = await _inventoryService.ReserveStockAsync(order, ct);

                timer.Stop();

                if (!reserved)
                {
                    string productName = await GetInsufficientStockProductNameAsync(order, ct);

                    string reason = $"Insufficient stock for '{productName}'";

                    LogStep(order, 2, $"Inventory ... REJECTED ({reason})",timer.ElapsedMilliseconds);

                    return await FinishEarlyAsync(
                        order, OrderStatus.Rejected, reason, ct);
                }

                stockReserved = true;
                LogStep(order, 2, "Inventory ... OK", timer.ElapsedMilliseconds);

                // Step 3/6: Calculate total
                timer.Restart();

                try
                {
                    await _pricingService.CalculateTotalAsync(order, ct);
                }
                catch (InvalidCouponException ex)
                {
                    timer.Stop();

                    await _inventoryService.ReleaseStockAsync(order, ct);
                    stockReserved = false;

                    LogStep(order, 3, $"Pricing ... REJECTED ({ex.Message})", timer.ElapsedMilliseconds);

                    return await FinishEarlyAsync(order, OrderStatus.Rejected, ex.Message, ct);
                }

                timer.Stop();
                LogStep(order, 3, $"Pricing ... OK (Total {order.TotalAmount:F2})", timer.ElapsedMilliseconds);

                // Step 4/6: Payment, with a 3-second timeout
                timer.Restart();

                PaymentResult paymentResult;

                using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

                    try
                    {
                        paymentResult = await _paymentGateway.ProcessPaymentAsync(
                            order.Id,
                            order.TotalAmount,
                            timeoutCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        timer.Stop();

                        if (ct.IsCancellationRequested)
                        {
                            throw;
                        }

                        await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);
                        stockReserved = false;

                        const string reason = "Payment gateway timeout (3s)";

                        LogStep(order, 4,
                            $"Payment ... TIMEOUT after {timer.ElapsedMilliseconds} ms, released",
                            timer.ElapsedMilliseconds);

                        return await FinishEarlyAsync(
                            order, OrderStatus.PaymentFailed, reason,
                            CancellationToken.None);
                    }
                }

                timer.Stop();

                if (!paymentResult.Success ||
                    paymentResult.Status == PaymentStatus.Declined)
                {
                    await _inventoryService.ReleaseStockAsync(order, ct);
                    stockReserved = false;

                    const string reason = "Card declined by issuer";

                    LogStep(order, 4,
                        $"Payment ... DECLINED ({reason})",
                        timer.ElapsedMilliseconds);

                    return await FinishEarlyAsync(order, OrderStatus.PaymentFailed, reason, ct);
                }

                transactionId = paymentResult.TransactionId;
                order.TransactionId = transactionId;

                LogStep(order, 4,
                    $"Payment ... OK ({transactionId})",
                    timer.ElapsedMilliseconds);

                // Step 5/6: Mark completed and save
                timer.Restart();

                order.Status = OrderStatus.Completed;
                order.FailureReason = null;

                try
                {
                    await _orderRepository.SaveAsync(order, ct);
                    timer.Stop();

                    LogStep(order, 5, "Save order ... OK",timer.ElapsedMilliseconds);
                }
                catch (DatabaseWriteException)
                {
                    timer.Stop();

                    await _paymentGateway.RefundAsync(transactionId!, CancellationToken.None);

                    await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);

                    stockReserved = false;

                    order.Status = OrderStatus.Failed;
                    order.FailureReason = $"Database save failed after payment; payment {transactionId} refunded";

                    await _orderRepository.SaveAsync(order, CancellationToken.None);

                    LogStep(order, 5,$"Save order ... FAILED; payment {transactionId} refunded, stock released", timer.ElapsedMilliseconds);

                    return false;
                }

               
                // Step 6/6: Email confirmation
                timer.Restart();

                await _emailService.SendConfirmationWithRetryAsync(order, customer, ct);

                timer.Stop();

                LogStep(
                    order,
                    6,
                    "Email processing finished",
                    timer.ElapsedMilliseconds);

                return order.Status == OrderStatus.Completed;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
              
                if (!string.IsNullOrWhiteSpace(transactionId))
                {
                    await _paymentGateway.RefundAsync(transactionId, CancellationToken.None);
                }

                if (stockReserved)
                {
                    await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);
                }

                order.Status = OrderStatus.Pending;
                order.FailureReason = "Cancelled";

                await _orderRepository.SaveAsync(order, CancellationToken.None);

                Log.Info($"Order #{order.Id}", "Processing cancelled; compensation completed");

                return false;
            }
        }

        private async Task<bool> FinishEarlyAsync(
            Order order,
            OrderStatus status,
            string reason,
            CancellationToken ct)
        {
            order.Status = status;
            order.FailureReason = reason;

            await _orderRepository.SaveAsync(order, ct);

            return false;
        }

        private async Task<string> GetInsufficientStockProductNameAsync(Order order, CancellationToken ct)
        {
            foreach (OrderItem item in order.Items)
            {
                Product? product = await _productRepository.GetByIdAsync(
                    item.ProductId, ct);

                if (product is null)
                {
                    continue;
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return product.Name;
                }
            }

            if (order.Items.Count > 0)
            {
                Product? product = await _productRepository.GetByIdAsync(order.Items[0].ProductId, ct);

                return product?.Name ?? $"Product #{order.Items[0].ProductId}";
            }

            return "Unknown product";
        }

        private void LogStep(
            Order order,
            int step,
            string message,
            long elapsedMilliseconds)
        {
            _usedThreads.TryAdd(Environment.CurrentManagedThreadId, 0);

            Log.Info(
                $"Order #{order.Id}",
                $"Step {step}/6 {message} in {elapsedMilliseconds} ms");
        }
    }
}
