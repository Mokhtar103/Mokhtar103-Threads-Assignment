using OrderProcessing.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Helpers
{
    public static class SyncSimulatedLatency
    {
        public static void Delay(string source, string step, int milliseconds)
        {
            Log.Info(source, $"{step} ({milliseconds} ms)");
            Thread.Sleep(milliseconds);
        }

        public static int GetQueryExecutionTime()
        {
            return Random.Shared.Next(300, 601);
        }
    }
}
