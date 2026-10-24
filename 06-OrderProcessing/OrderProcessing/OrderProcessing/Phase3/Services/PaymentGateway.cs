using OrderProcessing.Shared.Helpers;
using OrderProcessing.Shared.Interfaces;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public sealed class PaymentGateway : IPaymentGateway
    {
       
        private readonly JsonFileStore _fileStore;
        private readonly HashSet<int> _timeoutOrderIds;
        private readonly HashSet<int> _declineOrderIds;

        public PaymentGateway(JsonFileStore fileStore,
            IEnumerable<int>? timeoutOrderIds = null,
            IEnumerable<int>? declineOrderIds = null,
            bool failuresEnabled = true)
        {
            _fileStore = fileStore;
            _timeoutOrderIds = failuresEnabled
                                ? new HashSet<int>(timeoutOrderIds ?? Array.Empty<int>())
                                : new HashSet<int>();

            _declineOrderIds = failuresEnabled
                                ? new HashSet<int>(declineOrderIds ?? Array.Empty<int>())
                                : new HashSet<int>();
        }

        private async Task AppendPaymentAsync(
            int orderId,
            decimal amount,
            PaymentResult result,
            CancellationToken ct)
        {
            var transaction = new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = result.Status,
                TransactionId = result.TransactionId,
                CreatedAt = DateTime.Now
            };

            await _fileStore.UpdateAsync<PaymentTransaction, bool>(
                DataFiles.Payments,
                payments =>
                {
                    payments.Add(transaction);
                    return true;
                },
                ct);
        }

        public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync("PaymentGateway", "Connecting to payment gateway...", 150, ct);

            await SimulatedLatency.DelayAsync("PaymentGateway", "Running fraud check...", 150, ct);

            if (_declineOrderIds.Contains(orderId))
            {
                var declined = new PaymentResult
                {
                    Success = false,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };

                await AppendPaymentAsync(orderId, amount, declined, ct);
                return declined;
            }

            int captureDelay = _timeoutOrderIds.Contains(orderId) ? 5000 : 700;

            await SimulatedLatency.DelayAsync("PaymentGateway", "Capturing payment...", captureDelay, ct);

            ct.ThrowIfCancellationRequested();

            string transactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

            var result = new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured"
            };

            await AppendPaymentAsync(orderId, amount, result, ct);
            return result;
        }

        public async Task RefundAsync(
            string transactionId,
            CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync(
                "PaymentGateway", "Processing refund...", 700, ct);

            await _fileStore.UpdateAsync<PaymentTransaction, bool>(
                DataFiles.Payments,
                payments =>
                {
                    PaymentTransaction? payment = null;

                    foreach (PaymentTransaction candidate in payments)
                    {
                        if (candidate.TransactionId == transactionId)
                        {
                            payment = candidate;
                            break;
                        }
                    }

                    if (payment is null)
                    {
                        throw new InvalidOperationException($"Transaction '{transactionId}' not found");
                    }

                    payment.Status = PaymentStatus.Refunded;
                    return true;
                },
                ct);
        }

        

    }
}
