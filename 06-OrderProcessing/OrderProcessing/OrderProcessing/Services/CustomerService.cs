using OrderProcessing.Exceptions;
using OrderProcessing.Helpers;
using OrderProcessing.Models;
using OrderProcessing.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Services
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
