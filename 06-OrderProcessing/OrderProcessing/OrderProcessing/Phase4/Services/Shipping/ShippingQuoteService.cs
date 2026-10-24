using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Interfaces;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase4.Services.Shipping
{
    public sealed class ShippingQuoteService
    {
        private static readonly TimeSpan TimeBudget = TimeSpan.FromMilliseconds(1200);

        private readonly List<IShippingCarrier> _carriers;

        public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
        {
            _carriers = new List<IShippingCarrier>(carriers);

            if (_carriers.Count == 0)
            {
                throw new ArgumentException("At least one shipping carrier is required.", nameof(carriers));
            }
        }

        public async Task<ShippingQuote> GetFastestQuoteAsync(
            Order order,
            CancellationToken ct)
        {
            var timer = Stopwatch.StartNew();

            using var carrierCts =
                CancellationTokenSource.CreateLinkedTokenSource(ct);

            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(ct);

            Task timeoutTask = Task.Delay(
                TimeSpan.FromMilliseconds(1200),
                timeoutCts.Token);

            var pending = new List<(
                IShippingCarrier Carrier,
                Task<decimal> Task)>();

            // Start all carriers at the same time.
            foreach (IShippingCarrier carrier in _carriers)
            {
                pending.Add((
                    carrier,
                    carrier.GetQuoteAsync(order, carrierCts.Token)));
            }

            try
            {
                while (pending.Count > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    Task<decimal>[] carrierTasks = pending
                        .ConvertAll(item => item.Task)
                        .ToArray();

                    Task<decimal> firstFinishedTask =
                        await Task.WhenAny(carrierTasks);

                    // Race the next carrier completion against the deadline.
                    Task winner = await Task.WhenAny(
                        firstFinishedTask,
                        timeoutTask);

                    ct.ThrowIfCancellationRequested();

                    if (winner == timeoutTask)
                    {
                        List<string> cancelled =
                            GetRunningCarrierNames(pending);

                        carrierCts.Cancel();

                        return new ShippingQuote
                        {
                            Carrier = "Flat rate",
                            Price = 25.00m,
                            ElapsedMs = timer.ElapsedMilliseconds,
                            CancelledCarriers = cancelled
                        };
                    }

                    int finishedIndex = pending.FindIndex(
                        item => item.Task == firstFinishedTask);

                    if (finishedIndex < 0)
                        continue;

                    IShippingCarrier finishedCarrier =
                        pending[finishedIndex].Carrier;

                    pending.RemoveAt(finishedIndex);

                    try
                    {
                        decimal price = await firstFinishedTask;

                        // First successful quote wins.
                        List<string> cancelled =
                            GetRunningCarrierNames(pending);

                        carrierCts.Cancel();
                        timeoutCts.Cancel();

                        return new ShippingQuote
                        {
                            Carrier = finishedCarrier.Name,
                            Price = price,
                            ElapsedMs = timer.ElapsedMilliseconds,
                            CancelledCarriers = cancelled
                        };
                    }
                    catch (ShippingProviderUnavailableException ex)
                    {
                        Log.Error(
                            finishedCarrier.Name,
                            $"Quote failed: {ex.Message}");

                        // Keep waiting for the remaining carriers.
                    }
                    catch (OperationCanceledException)
                        when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (OperationCanceledException)
                    {
                        // A carrier was cancelled; continue checking
                        // the remaining carriers or the timeout.
                    }
                }

                ct.ThrowIfCancellationRequested();

                throw new ShippingProviderUnavailableException(
                    "All carriers failed");
            }
            finally
            {
                carrierCts.Cancel();
                timeoutCts.Cancel();
            }
        }

        private static List<string> GetRunningCarrierNames(List<(IShippingCarrier Carrier, Task<decimal> Task)> pending)
        {
            var names = new List<string>();

            foreach (var item in pending)
            {
                if (!item.Task.IsCompleted)
                {
                    names.Add(item.Carrier.Name);
                }
            }

            return names;
        }

        
    }
}
