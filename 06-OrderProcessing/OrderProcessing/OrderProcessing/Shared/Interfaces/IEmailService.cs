using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct);

        Task SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken ct);
    }
}
