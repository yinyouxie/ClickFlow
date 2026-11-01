using System.Text.Json.Serialization;

namespace ClickFlow.Core;

public enum MacroEventKind
{
    Move,
    Down,
    Up,
    Wheel,
    KeyDown,
    KeyUp
}

public enum MouseButtonKind
{
    Left = 0,
    Right = 1,
    Middle = 2,
    X1 = 3,
    X2 = 4
}

public sealed class MacroEvent
{
    public long T { get; set; }
    public MacroEventKind Kind { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Data { get; set; }
    public int Scan { get; set; }
    public bool Extended { get; set; }
    public bool Horizontal { get; set; }

    [JsonIgnore]
    public MouseButtonKind Button => (MouseButtonKind)Data;
}

public sealed class MacroDocument
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "未命名宏";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string AppVersion { get; set; } = "1.1.4";
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public List<MacroEvent> Events { get; set; } = new();

    [JsonIgnore]
    public bool IsEmpty => Events.Count == 0;

    [JsonIgnore]
    public long DurationMs => Events.Count == 0 ? 0 : Events[^1].T;

    public MacroDocument Clone()
    {
        return new MacroDocument
        {
            Version = Version,
            Name = Name,
            CreatedUtc = CreatedUtc,
            AppVersion = AppVersion,
            ScreenWidth = ScreenWidth,
            ScreenHeight = ScreenHeight,
            Events = Events.Select(e => new MacroEvent
            {
                T = e.T,
                Kind = e.Kind,
                X = e.X,
                Y = e.Y,
                Data = e.Data,
                Scan = e.Scan,
                Extended = e.Extended,
                Horizontal = e.Horizontal
            }).ToList()
        };
    }
}

public sealed class PlaybackOptions
{
    public double Speed { get; set; } = 1.0;
    public int Loops { get; set; } = 1;
    public int StartDelayMs { get; set; }
    public bool RestoreCursor { get; set; } = true;
    public int LoopGapMs { get; set; } = 260;
}

public sealed record PlaybackProgress(
    int Loop,
    int Loops,
    int EventIndex,
    int EventCount,
    double Percent,
    long ElapsedMs);

public sealed class AutoClickOptions
{
    public bool UseCurrentPosition { get; set; } = true;
    public int X { get; set; }
    public int Y { get; set; }
    public MouseButtonKind Button { get; set; } = MouseButtonKind.Left;
    public int IntervalMs { get; set; } = 100;
    public int ClickCount { get; set; }
    public int JitterPx { get; set; }
}

public readonly record struct WindowPoint(int X, int Y);

public enum RawInputKind
{
    MouseMove,
    MouseDown,
    MouseUp,
    Wheel,
    KeyDown,
    KeyUp
}

public readonly record struct RawInputEvent(
    RawInputKind Kind,
    int X,
    int Y,
    int Data,
    int Scan,
    bool Extended,
    bool Horizontal,
    int VirtualKey);
