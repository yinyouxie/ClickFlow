using System.Diagnostics;
using System.Drawing;

namespace ClickFlow.Core;

internal sealed class MacroRecorder : IDisposable
{
    private readonly object _sync = new();
    private readonly List<MacroEvent> _events = new();
    private readonly Stopwatch _clock = new();
    private readonly GlobalHookService _hooks = new();
    private Func<Point, bool>? _ignorePoint;
    private bool _recording;
    private bool _disposed;
    private bool _limitNotified;
    private long _lastMoveAt = -10000;
    private int _lastMoveX;
    private int _lastMoveY;

    public int MaxEvents { get; set; } = 200_000;

    public bool RecordMouseMoves { get; set; } = true;

    public bool IsRecording => _recording;

    public long ElapsedMs => _recording ? _clock.ElapsedMilliseconds : Math.Max(0, _clock.ElapsedMilliseconds);

    public int EventCount
    {
        get
        {
            lock (_sync)
            {
                return _events.Count;
            }
        }
    }

    public event Action? EventLimitReached;

    public bool Start(Func<Point, bool>? ignorePoint, out string error)
    {
        error = string.Empty;
        if (_disposed)
        {
            error = "录制器已经释放。";
            return false;
        }

        if (_recording)
        {
            return true;
        }

        lock (_sync)
        {
            _events.Clear();
        }

        _ignorePoint = ignorePoint;
        _limitNotified = false;
        _lastMoveAt = -10000;
        _lastMoveX = int.MinValue;
        _lastMoveY = int.MinValue;

        _hooks.Input -= OnInput;
        _hooks.Input += OnInput;
        if (!_hooks.Start(out var hookError))
        {
            _hooks.Input -= OnInput;
            error = hookError;
            return false;
        }

        _clock.Restart();
        _recording = true;
        return true;
    }

    public List<MacroEvent> Stop()
    {
        if (!_recording)
        {
            return GetEvents();
        }

        _recording = false;
        _clock.Stop();
        _hooks.Stop();
        _hooks.Input -= OnInput;
        return GetEvents();
    }

    public List<MacroEvent> GetEvents()
    {
        lock (_sync)
        {
            return _events.ToArray().ToList();
        }
    }

    private void OnInput(RawInputEvent input)
    {
        if (!_recording)
        {
            return;
        }

        var isMouse = input.Kind is RawInputKind.MouseMove
            or RawInputKind.MouseDown or RawInputKind.MouseUp or RawInputKind.Wheel;

        if (isMouse && _ignorePoint != null &&
            _ignorePoint(new Point(input.X, input.Y)))
        {
            return;
        }

        if (!isMouse && KeyNames.IsHotkey(input.VirtualKey))
        {
            return;
        }

        var now = _clock.ElapsedMilliseconds;

        if (input.Kind == RawInputKind.MouseMove)
        {
            if (!RecordMouseMoves)
            {
                return;
            }

            if (_lastMoveX != int.MinValue)
            {
                var dx = Math.Abs(input.X - _lastMoveX);
                var dy = Math.Abs(input.Y - _lastMoveY);
                if (dx + dy == 0)
                {
                    return;
                }

                if (now - _lastMoveAt < 12 && dx + dy < 5)
                {
                    return;
                }
            }

            _lastMoveAt = now;
            _lastMoveX = input.X;
            _lastMoveY = input.Y;
        }
        else if (input.Kind is RawInputKind.MouseDown or RawInputKind.MouseUp)
        {
            _lastMoveAt = now;
            _lastMoveX = input.X;
            _lastMoveY = input.Y;
        }

        var evt = new MacroEvent
        {
            T = now,
            Kind = input.Kind switch
            {
                RawInputKind.MouseMove => MacroEventKind.Move,
                RawInputKind.MouseDown => MacroEventKind.Down,
                RawInputKind.MouseUp => MacroEventKind.Up,
                RawInputKind.Wheel => MacroEventKind.Wheel,
                RawInputKind.KeyDown => MacroEventKind.KeyDown,
                RawInputKind.KeyUp => MacroEventKind.KeyUp,
                _ => MacroEventKind.Move
            },
            X = input.X,
            Y = input.Y,
            Data = input.Data,
            Scan = input.Scan,
            Extended = input.Extended,
            Horizontal = input.Horizontal
        };

        var notifyLimit = false;
        lock (_sync)
        {
            if (_events.Count >= MaxEvents)
            {
                if (!_limitNotified)
                {
                    _limitNotified = true;
                    notifyLimit = true;
                }
            }
            else
            {
                _events.Add(evt);
            }
        }

        if (notifyLimit)
        {
            EventLimitReached?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _hooks.Dispose();
    }
}
