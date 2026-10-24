using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Helpers;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4.Services
{
    public sealed class CustomerDashboardService
    {
        private readonly JsonFileStore _fileStore;
        private readonly HashSet<int> _failPaymentsForCustomerIds;

        public CustomerDashboardService(
            JsonFileStore fileStore,
            IEnumerable<int>? failPaymentsForCustomerIds = null)
        {
            _fileStore = fileStore;

            _failPaymentsForCustomerIds = new HashSet<int>(
                failPaymentsForCustomerIds ?? Array.Empty<int>());
        }

        public async Task<CustomerDashboard> LoadAsync(int customerId, CancellationToken ct)
        {
            Task<Customer> profileTask = LoadProfileAsync(customerId, ct);
            Task<List<Order>> ordersTask = LoadOrdersAsync(customerId, ct);
            Task<List<PaymentTransaction>> paymentsTask = LoadPaymentsAsync(customerId, ct);
            Task<List<EmailMessage>> emailsTask = LoadEmailsAsync(customerId, ct);

            Task[] tasks =
            {
            profileTask,
            ordersTask,
            paymentsTask,
            emailsTask
        };

            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
                var errors = new List<Exception>();

                foreach (Task task in tasks)
                {
                    if (task.Exception is not null)
                    {
                        errors.AddRange(task.Exception.Flatten().InnerExceptions);
                    }
                }

                if (errors.Count > 0)
                {
                    throw new AggregateException("One or more dashboard loads failed", errors);
                }

                throw;
            }

            return new CustomerDashboard
            {
                Customer = await profileTask,
                Orders = await ordersTask,
                Payments = await paymentsTask,
                Emails = await emailsTask
            };
        }

        private async Task<Customer> LoadProfileAsync(int customerId, CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync(
                "Dashboard",
                "Loading customer profile",
                SimulatedLatency.GetQueryExecutionTime(),
                ct);

            List<Customer> customers =await _fileStore.ReadAllAsync<Customer>(DataFiles.Customers, ct);

            Customer? customer = customers.Find(c => c.Id == customerId);

            return customer ?? throw new CustomerNotFoundException(customerId);
        }

        private async Task<List<Order>> LoadOrdersAsync(int customerId, CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync(
                "Dashboard",
                "Loading customer orders",
                SimulatedLatency.GetQueryExecutionTime(),
                ct);

            List<Order> orders = await _fileStore.ReadAllAsync<Order>(DataFiles.Orders, ct);

            return orders.FindAll(o => o.CustomerId == customerId);
        }

        private async Task<List<PaymentTransaction>> LoadPaymentsAsync(int customerId, CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync(
                "Dashboard",
                "Loading customer payments",
                SimulatedLatency.GetQueryExecutionTime(),
                ct);

            if (_failPaymentsForCustomerIds.Contains(customerId))
            {
                throw new TimeoutException("Payments store timeout");
            }

            
            List<Order> orders = await _fileStore.ReadAllAsync<Order>(DataFiles.Orders, ct);

            HashSet<int> customerOrderIds = new(
                orders
                    .FindAll(o => o.CustomerId == customerId)
                    .ConvertAll(o => o.Id));

            List<PaymentTransaction> payments = await _fileStore.ReadAllAsync<PaymentTransaction>(DataFiles.Payments, ct);

            return payments.FindAll(p => customerOrderIds.Contains(p.OrderId));
        }

        private async Task<List<EmailMessage>> LoadEmailsAsync(int customerId, CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync(
                "Dashboard",
                "Loading customer emails",
                SimulatedLatency.GetQueryExecutionTime(),
                ct);

            List<Order> orders = await _fileStore.ReadAllAsync<Order>(DataFiles.Orders, ct);

            HashSet<int> customerOrderIds = new(
                orders
                    .FindAll(o => o.CustomerId == customerId)
                    .ConvertAll(o => o.Id));

            List<EmailMessage> emails = await _fileStore.ReadAllAsync<EmailMessage>(DataFiles.Emails, ct);

            return emails.FindAll(e => customerOrderIds.Contains(e.OrderId));
        }
    }
}
