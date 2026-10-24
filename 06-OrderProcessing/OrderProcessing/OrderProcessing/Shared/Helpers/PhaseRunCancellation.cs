using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Helpers
{
    public sealed class PhaseRunCancellation : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;

        public CancellationToken Token => _cts.Token;

        public PhaseRunCancellation(int? cancelAfterMs = null)
        {
            if (cancelAfterMs is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cancelAfterMs), "Cancellation delay cannot be negative");
            }

            Console.CancelKeyPress += OnCancelKeyPress;

            if (cancelAfterMs.HasValue)
            {
                _cts.CancelAfter(cancelAfterMs.Value);
            }
        }

        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;

            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Console.CancelKeyPress -= OnCancelKeyPress;
            _cts.Dispose();
            _disposed = true;
        }
    }
}
