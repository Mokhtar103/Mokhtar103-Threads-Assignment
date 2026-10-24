using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Models
{
    public sealed class ShippingQuote
    {
        public string Carrier { get; init; } = string.Empty;

        public decimal Price { get; init; }

        public long ElapsedMs { get; init; }

        public List<string> CancelledCarriers { get; init; } = new();
    }
}
