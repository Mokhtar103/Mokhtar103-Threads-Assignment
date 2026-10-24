using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4.Services.Shipping
{
    public sealed class FastShip : ShippingCarrierBase
    {
        public FastShip(IEnumerable<int>? failForOrderIds = null)
        : base(failForOrderIds)
        {
        }

        public override string Name => "FastShip";

        protected override int MinResponseMs => 500;
        protected override int MaxResponseMs => 1500;
        protected override decimal PriceFactor => 1.00m;
    }
}
