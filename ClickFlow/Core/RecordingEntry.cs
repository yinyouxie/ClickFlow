using System.Text.Json.Serialization;

namespace ClickFlow.Core;

public sealed class RecordingEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = string.Empty;
    public string Name { get; set; } = "未命名录制";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public long DurationMs { get; set; }
    public int ActionCount { get; set; }
    public int ClickCount { get; set; }
    public int DragCount { get; set; }
    public int KeyCount { get; set; }
    public int ScrollCount { get; set; }
    public int EventCount { get; set; }
    public long FileSize { get; set; }

    [JsonIgnore]
    public string FilePath => Path.Combine(RecordingLibrary.FolderPath, FileName);

    [JsonIgnore]
    public DateTime CreatedLocal => CreatedUtc.ToLocalTime();

    [JsonIgnore]
    public string Summary =>
        $"{FormatDuration(DurationMs)} · {ActionCount} 个操作";

    [JsonIgnore]
    public string DetailSummary =>
        $"点击 {ClickCount} · 拖动 {DragCount} · 滚轮 {ScrollCount} · 按键 {KeyCount}";

    public static string FormatDuration(long ms)
    {
        var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds / 10:00}";
    }
}

internal sealed class RecordingIndex
{
    public int Version { get; set; } = 1;
    public List<RecordingEntry> Entries { get; set; } = new();
}
