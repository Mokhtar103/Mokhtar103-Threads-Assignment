using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4.Services.Shipping
{
    public sealed class CairoExpress : ShippingCarrierBase
    {
        public CairoExpress(IEnumerable<int>? failForOrderIds = null)
        : base(failForOrderIds)
        {
        }

        public override string Name => "CairoExpress";

        protected override int MinResponseMs => 300;
        protected override int MaxResponseMs => 2000;
        protected override decimal PriceFactor => 0.95m;

        protected override bool HasBuiltInFailure(Order order)
        {
            return order.Id % 2 == 0;
        }
    }
}
