using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Exceptions
{
    public sealed class CustomerNotFoundException : Exception
    {
        public CustomerNotFoundException(int customerId)
        : base($"Customer #{customerId} not found")
        {
            CustomerId = customerId;
        }

        public int CustomerId { get; }
    }
}
