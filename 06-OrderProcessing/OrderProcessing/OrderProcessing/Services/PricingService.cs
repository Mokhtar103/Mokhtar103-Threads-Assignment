using OrderProcessing.Exceptions;
using OrderProcessing.Helpers;
using OrderProcessing.Logger;
using OrderProcessing.Models;
using OrderProcessing.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Services
{
    public sealed class PricingService
    {
        private readonly ProductRepository _productRepository;
        private readonly CustomerRepository _customerRepository;
        private readonly CouponRepository _couponRepository;
        private readonly HashSet<int> _failShippingForOrderIds;

        public PricingService(
            ProductRepository productRepository,
            CustomerRepository customerRepository,
            CouponRepository couponRepository,
            IEnumerable<int>? failShippingForOrderIds = null,
             bool failuresEnabled = true)
        {
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _couponRepository = couponRepository;

            _failShippingForOrderIds = failuresEnabled
                                        ? new HashSet<int>(
                                            failShippingForOrderIds ?? Array.Empty<int>())
                                        : new HashSet<int>();
        }

        // helper method for rounding
        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
        {
            const string source = "PricingService";

            await SimulatedLatency.DelayAsync(source, "Opening pricing connection...", 200, ct);

            await SimulatedLatency.DelayAsync(
                source,
                "Executing subtotal query...",
                SimulatedLatency.GetQueryExecutionTime(),
                ct);

            decimal subTotal = 0m;

            foreach (OrderItem item in order.Items)
            {
                Product? product = await _productRepository.GetByIdAsync(item.ProductId, ct);

                if (product is null)
                {
                    throw new InvalidOperationException($"Product #{item.ProductId} not found");
                }

                item.UnitPrice = RoundMoney(product.Price);

                subTotal += item.UnitPrice * item.Quantity;
            }

            subTotal = RoundMoney(subTotal);
            order.SubTotal = subTotal;

            Log.Success(source, $"Order #{order.Id} subtotal: {subTotal:F2}");

            return subTotal;
        }

        public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
        {
            const string source = "PricingService";

            await SimulatedLatency.DelayAsync(source, "Discount API: DNS/handshake", 150, ct);

            await SimulatedLatency.DelayAsync(source, "Discount API: Sending request", 300, ct);

            await SimulatedLatency.DelayAsync(
                source,
                "Discount API: Waiting for response",
                Random.Shared.Next(500, 1501),
                ct);

            Customer? customer = await _customerRepository.GetByIdAsync(order.CustomerId, ct);

            if (customer is null)
            {
                throw new InvalidOperationException($"Customer #{order.CustomerId} not found");
            }

            decimal loyaltyDiscountPercent = customer.LoyaltyTier switch
            {
                LoyaltyTier.Bronze => 0m,
                LoyaltyTier.Silver => 5m,
                LoyaltyTier.Gold => 10m,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(customer.LoyaltyTier))
            };

            decimal couponDiscountPercent = 0m;

            if (order.CouponCode is not null)
            {
                Coupon? coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, ct);

                if (coupon is null)
                {
                    throw new InvalidCouponException($"Coupon '{order.CouponCode}' does not exist");
                }

                if (coupon.ExpiryDate < DateTime.Today)
                {
                    throw new InvalidCouponException($"Coupon '{order.CouponCode}' has expired");
                }

                couponDiscountPercent = coupon.DiscountPercent;
            }

            decimal totalDiscountPercent = Math.Min(loyaltyDiscountPercent + couponDiscountPercent, 30m);

            decimal discount = RoundMoney(order.SubTotal * totalDiscountPercent / 100m);

            Log.Success(
                source,
                $"Order #{order.Id} discount: {discount:F2} " +
                $"({totalDiscountPercent:F0}%)");

            return discount;
        }

        public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
        {
            const string source = "PricingService";

            await SimulatedLatency.DelayAsync(source, "Shipping API: DNS/handshake", 150, ct);

            if (_failShippingForOrderIds.Contains(order.Id))
            {
                Log.Error(source, $"Shipping provider unavailable for order #{order.Id}.");

                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }

            await SimulatedLatency.DelayAsync(source, "Shipping API: Sending request", 300, ct);

            await SimulatedLatency.DelayAsync(
                source,
                "Shipping API: Waiting for response",
                Random.Shared.Next(500, 1501),
                ct);

            // rule : 0 when SubTotal ≥ 1000
            if (order.SubTotal >= 1000m)
            {
                Log.Success(source,$"Order #{order.Id} qualifies for free shipping");

                return 0m;
            }

            int totalQuantity = 0;
            decimal totalWeight = 0m;

            foreach (OrderItem item in order.Items)
            {
                Product? product =await _productRepository.GetByIdAsync(item.ProductId, ct);

                if (product is null)
                {
                    throw new InvalidOperationException($"Product #{item.ProductId} not found");
                }

                totalQuantity += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            // rule : 5.00 + 1.50 × total quantity + 2.00 × total weight (kg)
            decimal shippingCost =
                5.00m
                + 1.50m * totalQuantity
                + 2.00m * totalWeight;

            shippingCost = RoundMoney(shippingCost);

            Log.Success(source, $"Order #{order.Id} shipping: {shippingCost:F2}");

            return shippingCost;
        }

        public async Task<decimal> CalculateTotalAsync(Order order, CancellationToken ct)
        {
            await CalculateSubTotalAsync(order, ct);

            var discountTask = GetDiscountAsync(order, ct);
            var shippingTask = CalculateShippingCostAsync(order, ct);

            await Task.WhenAll(discountTask, shippingTask);

            order.Discount = await discountTask;
            order.ShippingCost = await shippingTask;

            order.TotalAmount = RoundMoney(order.SubTotal - order.Discount + order.ShippingCost);

            return order.TotalAmount;
        }
    }
}
