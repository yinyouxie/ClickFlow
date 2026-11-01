using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace ClickFlow.Core;

internal sealed class MouseKeyboardPlayer
{
    private readonly object _gate = new();
    private readonly Dictionary<int, HeldKey> _heldKeys = new();
    private readonly HashSet<MouseButtonKind> _heldButtons = new();
    private volatile bool _playing;
    private volatile bool _pointerDown;

    public bool IsPlaying => _playing;

    public bool IsPointerDown => _pointerDown;

    public Task PlayAsync(
        MacroDocument document,
        PlaybackOptions options,
        IProgress<PlaybackProgress>? progress,
        CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(
            () => PlayCore(document, options, progress, cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private void PlayCore(
        MacroDocument document,
        PlaybackOptions options,
        IProgress<PlaybackProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (document.Events.Count == 0)
        {
            return;
        }

        _playing = true;
        var speed = Math.Clamp(options.Speed, 0.1, 10.0);
        var loops = Math.Clamp(options.Loops, 1, 9999);
        var original = InputInjector.GetCursorPosition();
        var eventCount = document.Events.Count;
        NativeMethods.TimeBeginPeriod(1);

        try
        {
            for (var loop = 1; loop <= loops; loop++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var clock = Stopwatch.StartNew();
                var lastReport = -1000L;

                for (var i = 0; i < eventCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var evt = document.Events[i];
                    var target = (long)Math.Round(evt.T / speed);
                    WaitUntil(clock, target, cancellationToken);
                    Inject(evt);

                    var now = clock.ElapsedMilliseconds;
                    if (progress != null &&
                        (i == eventCount - 1 || now - lastReport >= 90))
                    {
                        lastReport = now;
                        var fraction = (loop - 1 + (i + 1.0) / eventCount) / loops;
                        progress.Report(new PlaybackProgress(
                            loop,
                            loops,
                            i + 1,
                            eventCount,
                            Math.Clamp(fraction * 100.0, 0, 100),
                            document.DurationMs));
                    }
                }

                ReleaseHeld();
                progress?.Report(new PlaybackProgress(
                    loop,
                    loops,
                    eventCount,
                    eventCount,
                    loop / (double)loops * 100.0,
                    document.DurationMs));

                if (loop < loops)
                {
                    WaitUntil(Stopwatch.StartNew(), options.LoopGapMs, cancellationToken);
                }
            }
        }
        finally
        {
            try
            {
                ReleaseHeld();
            }
            catch
            {
                // Never mask cancellation with a failed safety release.
            }

            if (options.RestoreCursor)
            {
                try
                {
                    InputInjector.MoveTo(original.X, original.Y);
                }
                catch
                {
                    // Cursor restoration is best effort.
                }
            }

            NativeMethods.TimeEndPeriod(1);
            _playing = false;
        }
    }

    private void Inject(MacroEvent evt)
    {
        switch (evt.Kind)
        {
            case MacroEventKind.Move:
                InputInjector.MoveTo(evt.X, evt.Y);
                break;
            case MacroEventKind.Down:
                MoveToIfNeeded(evt.X, evt.Y);
                InputInjector.MouseButton((MouseButtonKind)evt.Data, true);
                _pointerDown = true;
                lock (_gate)
                {
                    _heldButtons.Add((MouseButtonKind)evt.Data);
                }

                break;
            case MacroEventKind.Up:
                MoveToIfNeeded(evt.X, evt.Y);
                InputInjector.MouseButton((MouseButtonKind)evt.Data, false);
                if (_heldButtons.Count == 0)
                {
                    _pointerDown = false;
                }

                lock (_gate)
                {
                    _heldButtons.Remove((MouseButtonKind)evt.Data);
                    _pointerDown = _heldButtons.Count > 0;
                }

                break;
            case MacroEventKind.Wheel:
                MoveToIfNeeded(evt.X, evt.Y);
                InputInjector.Wheel(evt.Data, evt.Horizontal);
                break;
            case MacroEventKind.KeyDown:
                InputInjector.Key(evt.Data, evt.Scan, evt.Extended, true);
                lock (_gate)
                {
                    _heldKeys[evt.Data] = new HeldKey(evt.Scan, evt.Extended);
                }

                break;
            case MacroEventKind.KeyUp:
                InputInjector.Key(evt.Data, evt.Scan, evt.Extended, false);
                lock (_gate)
                {
                    _heldKeys.Remove(evt.Data);
                }

                break;
        }
    }

    private static void MoveToIfNeeded(int x, int y)
    {
        var current = InputInjector.GetCursorPosition();
        if (Math.Abs(current.X - x) > 1 || Math.Abs(current.Y - y) > 1)
        {
            InputInjector.MoveTo(x, y);
        }
    }

    private void ReleaseHeld()
    {
        List<MouseButtonKind> buttons;
        List<KeyValuePair<int, HeldKey>> keys;
        lock (_gate)
        {
            buttons = _heldButtons.ToList();
            keys = _heldKeys.ToList();
            _heldButtons.Clear();
            _heldKeys.Clear();
            _pointerDown = false;
        }

        foreach (var button in buttons)
        {
            try
            {
                InputInjector.MouseButton(button, false);
            }
            catch
            {
                // Best effort.
            }
        }

        foreach (var key in keys)
        {
            try
            {
                InputInjector.Key(key.Key, key.Value.Scan, key.Value.Extended, false);
            }
            catch
            {
                // Best effort.
            }
        }
    }

    private readonly record struct HeldKey(int Scan, bool Extended);

    private static void WaitUntil(
        Stopwatch clock,
        long targetMs,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = targetMs - clock.ElapsedMilliseconds;
            if (remaining <= 0)
            {
                return;
            }

            if (remaining > 18)
            {
                Thread.Sleep((int)Math.Min(remaining - 8, 12));
            }
            else if (remaining > 2)
            {
                Thread.Sleep(1);
            }
            else
            {
                Thread.SpinWait(80);
            }
        }
    }
}
