using OrderProcessing.Shared.Interfaces;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase4.Runners
{
    public sealed class EmailResendRunner
    {
        private const int FirstOrderId = 1001;
        private const int EmailCount = 30;
        private const int MaxConcurrency = 4;

        private readonly IEmailService _emailService;
        private readonly JsonFileStore _fileStore;

        public EmailResendRunner(IEmailService emailService, JsonFileStore fileStore)
        {
            _emailService = emailService;
            _fileStore = fileStore;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await _fileStore.UpdateAsync<EmailMessage, bool>(
                DataFiles.Emails,
                emails =>
                {
                    emails.RemoveAll(email =>
                        email.OrderId >= FirstOrderId &&
                        email.OrderId < FirstOrderId + EmailCount);

                    return true;
                },
                ct);

            var testData = new List<(Order Order, Customer Customer)>();

            for (int i = 0; i < EmailCount; i++)
            {
                int orderId = FirstOrderId + i;

                var customer = new Customer
                {
                    Id = orderId,
                    Name = $"Test Customer {orderId}",
                    Email = $"customer{orderId}@example.com",
                    IsActive = true
                };

                var order = new Order
                {
                    Id = orderId,
                    CustomerId = customer.Id,
                    TotalAmount = 100m,
                    Status = OrderStatus.Completed
                };

                testData.Add((order, customer));
            }

            int currentlySending = 0;
            int highestConcurrency = 0;

            var stopwatch = Stopwatch.StartNew();

            await Parallel.ForEachAsync(
                testData,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = MaxConcurrency,
                    CancellationToken = ct
                },
                async (item, token) =>
                {
                    int current = Interlocked.Increment(ref currentlySending);

                    UpdateMaximum(ref highestConcurrency, current);

                    try
                    {
                        await _emailService.SendConfirmationAsync(
                            item.Order,
                            item.Customer,
                            token);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref currentlySending);
                    }
                });

            stopwatch.Stop();

            var savedEmails =
                _fileStore.ReadAll<EmailMessage>(DataFiles.Emails);

            int savedTestEmails = 0;
            bool allExactlyOnce = true;

            for (int orderId = FirstOrderId;
                 orderId < FirstOrderId + EmailCount;
                 orderId++)
            {
                int matches = 0;

                foreach (var email in savedEmails)
                {
                    if (email.OrderId == orderId)
                    {
                        matches++;
                    }
                }

                if (matches != 1)
                {
                    allExactlyOnce = false;
                }

                savedTestEmails += matches;
            }

            Console.WriteLine();
            Console.WriteLine("=== Email Re-send Results ===");
            Console.WriteLine($"Expected emails: {EmailCount}");
            Console.WriteLine($"Saved test emails: {savedTestEmails}");
            Console.WriteLine($"Highest concurrency: {highestConcurrency}");
            Console.WriteLine($"Concurrency limit: {MaxConcurrency}");
            Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Each email saved exactly once: {allExactlyOnce}");

            if (savedTestEmails != EmailCount || !allExactlyOnce)
            {
                throw new InvalidOperationException("Email persistence verification failed.");
            }

            if (highestConcurrency != MaxConcurrency)
            {
                throw new InvalidOperationException(
                    $"Expected concurrency to reach {MaxConcurrency}, " +
                    $"but it reached {highestConcurrency}.");
            }
        }

        private static void UpdateMaximum(ref int maximum, int value)
        {
            int currentMaximum;

            do
            {
                currentMaximum = Volatile.Read(ref maximum);

                if (value <= currentMaximum)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(
                       ref maximum,
                       value,
                       currentMaximum) != currentMaximum);
        }
    }
}
