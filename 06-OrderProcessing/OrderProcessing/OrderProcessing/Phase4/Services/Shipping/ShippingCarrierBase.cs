using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Interfaces;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4.Services.Shipping
{
    public abstract class ShippingCarrierBase : IShippingCarrier
    {
        private readonly HashSet<int> _failForOrderIds;

        protected ShippingCarrierBase(IEnumerable<int>? failForOrderIds = null)
        {
            _failForOrderIds = new HashSet<int>(failForOrderIds ?? Array.Empty<int>());
        }

        public abstract string Name { get; }

        protected abstract int MinResponseMs { get; }
        protected abstract int MaxResponseMs { get; }
        protected abstract decimal PriceFactor { get; }

        protected virtual bool HasBuiltInFailure(Order order) => false;

        public async Task<decimal> GetQuoteAsync(Order order, CancellationToken ct)
        {
            try
            {
                await Task.Delay(150, ct);

                if (_failForOrderIds.Contains(order.Id) ||
                    HasBuiltInFailure(order))
                {
                    throw new ShippingProviderUnavailableException($"{Name} is unavailable for Order #{order.Id}");
                }

                int responseMs = Random.Shared.Next(
                    MinResponseMs,
                    MaxResponseMs + 1);

                await Task.Delay(responseMs, ct);

               
                return Math.Round(
                    order.ShippingCost * PriceFactor,
                    2,
                    MidpointRounding.AwayFromZero);
            }
            catch (OperationCanceledException)
            {
                Log.Info(Name, "cancelled");
                throw;
            }
        }
    }
}
