using OrderProcessing.Logger;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Helpers
{
    public static class SimulatedLatency
    {
        public static async Task DelayAsync(string source, string message, int milliseconds, CancellationToken ct)
        {
            Log.Info(source, $"{message} ({milliseconds} ms)");
            await Task.Delay(milliseconds, ct);
        }

        public static int GetQueryExecutionTime()
        {
            return Random.Shared.Next(300, 601);
        }

        public static int GetExternalResponseTime()
        {
            return Random.Shared.Next(500, 1501);
        }
    }
}
