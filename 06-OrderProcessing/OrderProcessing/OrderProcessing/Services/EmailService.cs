using OrderProcessing.Data;
using OrderProcessing.Exceptions;
using OrderProcessing.Helpers;
using OrderProcessing.Logger;
using OrderProcessing.Models;
using OrderProcessing.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Services
{
    public sealed class EmailService : IEmailService
    {
        private readonly JsonFileStore _fileStore;
        private readonly Dictionary<int, int> _failAttempts;
        private readonly ConcurrentDictionary<int, int> _attemptCounters = new();

        public EmailService(JsonFileStore fileStore,
            IReadOnlyDictionary<int, int>? failAttempts = null,
            bool failuresEnabled = true)
        {
            _fileStore = fileStore;
            _failAttempts = failuresEnabled && failAttempts is not null
                           ? new Dictionary<int, int>(failAttempts)
                           : new Dictionary<int, int>();
        }

        public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct)
        {
            int attempt = _attemptCounters.AddOrUpdate(
                order.Id,
                1,
                (_, current) => current + 1);

            await SimulatedLatency.DelayAsync("EmailService", $"Connecting to SMTP (attempt {attempt})...", 200, ct);

            if (_failAttempts.TryGetValue(order.Id, out int maxFailures) &&
                attempt <= maxFailures)
            {
                throw new SmtpUnavailableException($"SMTP server unavailable for order #{order.Id}, attempt {attempt}");
            }

            await SimulatedLatency.DelayAsync("EmailService", "Sending confirmation email...", 300, ct);

            var email = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} confirmation",
                Body = $"Your order #{order.Id} has been completed. Total: {order.TotalAmount:F2}",
                SentAt = DateTime.Now,
                Attempts = attempt,
                LastError = null
            };

            await _fileStore.UpdateAsync<EmailMessage, bool>(
                DataFiles.Emails,
                emails =>
                {
                    emails.Add(email);
                    return true;
                },
                ct);
        }

        public async Task SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken ct)
        {
            int[] retryDelays = { 500, 1000, 2000 };
            string? lastError = null;

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                try
                {
                    await SendConfirmationAsync(order, customer, ct);
                    return;
                }
                catch (SmtpUnavailableException ex)
                {
                    lastError = ex.Message;

                    Log.Error(
                        $"Email for Order #{order.Id}",
                        $"Attempt {attempt}/4 failed: {ex.Message}");

                    if (attempt == 4)
                    {
                        break;
                    }

                    await Task.Delay(retryDelays[attempt - 1], ct);
                }
            }

            var retryMessage = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} confirmation",
                Body = $"Your order #{order.Id} has been completed. Total: {order.TotalAmount:F2}",
                SentAt = DateTime.Now,
                Attempts = 4,
                LastError = lastError
            };

            await _fileStore.UpdateAsync<EmailMessage, bool>(
                DataFiles.EmailRetry,
                messages =>
                {
                    messages.Add(retryMessage);
                    return true;
                },
                ct);

            Log.Info(
                $"Email for Order #{order.Id}",
                "queued for retry later");
        }
    }
}
