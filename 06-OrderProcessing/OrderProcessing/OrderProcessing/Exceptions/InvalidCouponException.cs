using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Exceptions
{
    public sealed class InvalidCouponException : Exception
    {
        public InvalidCouponException(string message)
       : base(message)
        {

        }
    }
}
