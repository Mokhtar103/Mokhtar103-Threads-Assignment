using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Exceptions
{
    public sealed class SmtpUnavailableException : Exception
    {
        public SmtpUnavailableException(string message)
        : base(message)
        {

        }
    }
}
