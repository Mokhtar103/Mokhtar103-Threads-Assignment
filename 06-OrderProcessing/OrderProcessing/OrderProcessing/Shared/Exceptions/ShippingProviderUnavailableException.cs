using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Exceptions
{
    public sealed class ShippingProviderUnavailableException : Exception
    {
        public ShippingProviderUnavailableException(string message)
            :base(message)
        {

        }
    }
}
