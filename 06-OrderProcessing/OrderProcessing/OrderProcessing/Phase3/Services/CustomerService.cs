using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Helpers;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3.Services
{
    public sealed class CustomerService
    {
        private readonly CustomerRepository _customerRepository;

        public CustomerService(CustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<Customer> GetCustomerAsync(int id, CancellationToken ct)
        {
            await SimulatedLatency.DelayAsync(
            "CustomerService",
            "Querying customer...",
            200,
            ct);

            Customer? customer = await _customerRepository.GetByIdAsync(id, ct);

            if (customer is null)
            {
                throw new CustomerNotFoundException(id);
            }

            return customer;
        }
    }
}
