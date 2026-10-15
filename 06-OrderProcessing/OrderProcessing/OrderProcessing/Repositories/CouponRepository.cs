using OrderProcessing.Data;
using OrderProcessing.Helpers;
using OrderProcessing.Logger;
using OrderProcessing.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Repositories
{
    public sealed class CouponRepository
    {
        private readonly JsonFileStore _fileStore;

        public CouponRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct)
        {
            const string source = "CouponRepository";

            await SimulatedLatency.DelayAsync(source, "Opening connection...", 200, ct);

            await SimulatedLatency.DelayAsync(source, "Executing query...", SimulatedLatency.GetQueryExecutionTime(), ct);

            List<Coupon> coupons = await _fileStore.ReadAllAsync<Coupon>(DataFiles.Coupons, ct);

            Coupon? coupon = null;

            foreach (Coupon item in coupons)
            {
                if (string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    coupon = item;
                    break;
                }
            }

            if (coupon is null)
            {
                Log.Error(source, $"Coupon '{code}' not found");
            }
            else
            {
                Log.Success(source, $"Coupon '{code}' found");
            }

            return coupon;
        }
    }
}
