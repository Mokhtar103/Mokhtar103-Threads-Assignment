using OrderProcessing.Data;
using OrderProcessing.Helpers;
using OrderProcessing.Logger;
using OrderProcessing.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Repositories
{
    public sealed class SyncCouponRepository
    {
        private readonly JsonFileStore _fileStore;

        public SyncCouponRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public Coupon? GetByCode(string code)
        {
            const string source = "SyncCouponRepository";

            SyncSimulatedLatency.Delay(source, "Opening connection...", 200);

            SyncSimulatedLatency.Delay(source, "Executing query...", SyncSimulatedLatency.GetQueryExecutionTime());

            List<Coupon> coupons = _fileStore.ReadAll<Coupon>(DataFiles.Coupons);

            foreach (Coupon coupon in coupons)
            {
                if (string.Equals(coupon.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Success(source, $"Coupon '{code}' found");
                    return coupon;
                }
            }

            Log.Info(source, $"Coupon '{code}' not found");
            return null;
        }
    }
}
