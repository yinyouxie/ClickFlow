using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace ClickFlow.Core;

internal sealed class AutoClicker
{
    private long _count;
    private volatile bool _running;
    private MouseButtonKind? _downButton;

    public bool IsRunning => _running;

    public long Count => Interlocked.Read(ref _count);

    public Task RunAsync(
        AutoClickOptions options,
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(
            () => RunCore(options, progress, cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private void RunCore(
        AutoClickOptions options,
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref _count, 0);
        _running = true;
        var interval = Math.Clamp(options.IntervalMs, 10, 60_000);
        var total = Math.Max(0, options.ClickCount);
        var random = new Random();
        NativeMethods.TimeBeginPeriod(1);

        try
        {
            var next = Stopwatch.StartNew();
            var nextTick = 0L;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var position = options.UseCurrentPosition
                    ? InputInjector.GetCursorPosition()
                    : new Point(options.X, options.Y);

                if (options.JitterPx > 0)
                {
                    position.Offset(
                        random.Next(-options.JitterPx, options.JitterPx + 1),
                        random.Next(-options.JitterPx, options.JitterPx + 1));
                }

                if (!options.UseCurrentPosition || options.JitterPx > 0)
                {
                    InputInjector.MoveTo(position.X, position.Y);
                }

                _downButton = options.Button;
                InputInjector.MouseButton(options.Button, true);
                InputInjector.MouseButton(options.Button, false);
                _downButton = null;

                var count = Interlocked.Increment(ref _count);
                progress?.Report(count);

                if (total > 0 && count >= total)
                {
                    break;
                }

                nextTick += interval;
                var remaining = nextTick - next.ElapsedMilliseconds;
                while (remaining > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (remaining > 12)
                    {
                        Thread.Sleep((int)Math.Min(remaining - 4, 8));
                    }
                    else if (remaining > 1)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        Thread.SpinWait(50);
                    }

                    remaining = nextTick - next.ElapsedMilliseconds;
                }

                if (next.ElapsedMilliseconds - nextTick > interval * 4L)
                {
                    nextTick = next.ElapsedMilliseconds;
                }
            }
        }
        finally
        {
            if (_downButton is { } down)
            {
                try
                {
                    InputInjector.MouseButton(down, false);
                }
                catch
                {
                    // Best effort.
                }

                _downButton = null;
            }

            NativeMethods.TimeEndPeriod(1);
            _running = false;
        }
    }

    public void ResetCount()
    {
        Interlocked.Exchange(ref _count, 0);
    }
}
