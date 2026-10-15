using OrderProcessing.Models;
using OrderProcessing.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Services
{
    public sealed class SyncOrderValidator
    {
        private readonly SyncCustomerRepository _customerRepository;
        private readonly SyncProductRepository _productRepository;
        private readonly SyncCouponRepository _couponRepository;

        public SyncOrderValidator(
            SyncCustomerRepository customerRepository,
            SyncProductRepository productRepository,
            SyncCouponRepository couponRepository)
        {
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _couponRepository = couponRepository;
        }

        public void ValidateOrder(Order order)
        {
            order.Status = OrderStatus.Pending;
            order.FailureReason = null;

            // Customer exists
            Customer? customer = _customerRepository.GetById(order.CustomerId);

            if (customer is null)
            {
                Reject(order, $"Customer #{order.CustomerId} not found");
                return;
            }

            // Customer is active
            if (!customer.IsActive)
            {
                Reject(order, "Customer is inactive");
                return;
            }

            // Every product exists
            List<Product> products = new();

            foreach (OrderItem item in order.Items)
            {
                Product? product = _productRepository.GetById(item.ProductId);

                if (product is null)
                {
                    Reject(order, $"Product #{item.ProductId} not found");
                    return;
                }

                products.Add(product);
            }

            // Check stock without reserving it
            for (int i = 0; i < order.Items.Count; i++)
            {
                OrderItem item = order.Items[i];
                Product product = products[i];

                if (product.StockQuantity < item.Quantity)
                {
                    Reject(order, $"Insufficient stock for '{product.Name}'");

                    return;
                }
            }

            // Coupon exists
            if (order.CouponCode is not null)
            {
                Coupon? coupon = _couponRepository.GetByCode(order.CouponCode);

                if (coupon is null)
                {
                    Reject(order, $"Coupon '{order.CouponCode}' does not exist");

                    return;
                }

                // Coupon has not expired
                if (coupon.ExpiryDate < DateTime.Today)
                {
                    Reject(order, $"Coupon '{order.CouponCode}' has expired");

                    return;
                }
            }

            order.Status = OrderStatus.Validated;
            order.FailureReason = null;
        }

        private static void Reject(Order order, string reason)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = reason;
        }
    }
}
