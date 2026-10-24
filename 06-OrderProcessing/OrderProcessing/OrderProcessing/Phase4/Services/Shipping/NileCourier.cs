using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4.Services.Shipping
{
    public sealed class NileCourier : ShippingCarrierBase
    {
        public NileCourier(IEnumerable<int>? failForOrderIds = null)
       : base(failForOrderIds)
        {
        }

        public override string Name => "NileCourier";

        protected override int MinResponseMs => 800;
        protected override int MaxResponseMs => 1200;
        protected override decimal PriceFactor => 1.10m;
    }
}
