using OrderProcessing.Logger;
using OrderProcessing.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Services
{
    public sealed class OrderPricer
    {
        private readonly PricingService _pricingService;

        public OrderPricer(PricingService pricingService)
        {
            _pricingService = pricingService;
        }

        // helper method for rounding
        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        public async Task PriceOrderAsync(Order order, CancellationToken ct)
        {
            const string source = "OrderPricer";

            await _pricingService.CalculateSubTotalAsync(order, ct);

            Task<decimal> discountTask = _pricingService.GetDiscountAsync(order, ct);

            Task<decimal> shippingTask = _pricingService.CalculateShippingCostAsync(order, ct);

            // Wait for both operations to finish
            await Task.WhenAll(discountTask, shippingTask);

            order.Discount = await discountTask;
            order.ShippingCost = await shippingTask;

            order.TotalAmount = RoundMoney(
                order.SubTotal
                - order.Discount
                + order.ShippingCost);

            Log.Success(
                source,
                $"Order #{order.Id} priced concurrently. " +
                $"Total={order.TotalAmount:F2}");
        }

        public async Task PriceOrderSequentialAsync(
      Order order,
      CancellationToken ct)
        {
            const string source = "OrderPricer";

            await _pricingService.CalculateSubTotalAsync(order, ct);

            Task<decimal> discountTask = _pricingService.GetDiscountAsync(order, ct);

            order.Discount = await discountTask;

            Task<decimal> shippingTask = _pricingService.CalculateShippingCostAsync(order, ct);

            order.ShippingCost = await shippingTask;

            order.TotalAmount = RoundMoney(
                order.SubTotal
                - order.Discount
                + order.ShippingCost);

            Log.Success(
                source,
                $"Order #{order.Id} priced sequentially. " +
                $"Total={order.TotalAmount:F2}");
        }
    }
}
