using System.Drawing;

namespace ClickFlow.Core;

public enum DisplayActionKind
{
    Click,
    Drag,
    Scroll,
    Key
}

public sealed record DisplayAction(
    long StartMs,
    long DurationMs,
    DisplayActionKind Kind,
    string Title,
    string Detail,
    int Count = 1,
    int X = 0,
    int Y = 0,
    bool Horizontal = false);

public sealed class MacroSummary
{
    public int ActionCount { get; init; }
    public int ClickCount { get; init; }
    public int DragCount { get; init; }
    public int KeyCount { get; init; }
    public int ScrollCount { get; init; }
    public long DurationMs { get; init; }

    public static MacroSummary FromEvents(IReadOnlyList<MacroEvent> events)
    {
        var last = events.Count == 0 ? 0 : events[^1].T;
        var clicks = 0;
        var keys = 0;
        var scrolls = 0;

        foreach (var evt in events)
        {
            switch (evt.Kind)
            {
                case MacroEventKind.Down:
                    clicks++;
                    break;
                case MacroEventKind.Wheel:
                    scrolls++;
                    break;
                case MacroEventKind.KeyDown:
                    keys++;
                    break;
            }
        }

        return new MacroSummary
        {
            ActionCount = clicks + keys + scrolls,
            ClickCount = clicks,
            KeyCount = keys,
            ScrollCount = scrolls,
            DurationMs = last
        };
    }
}

public sealed class DisplayBuildResult
{
    public List<DisplayAction> Actions { get; init; } = new();
    public MacroSummary Summary { get; init; } = new();
}

/// <summary>
/// Turns low-level down/up/move records into human-readable clicks, drags,
/// scroll gestures and key presses for the timeline.
/// </summary>
public static class DisplayActionBuilder
{
    public static DisplayBuildResult Build(
        IReadOnlyList<MacroEvent> events,
        bool includePending = true)
    {
        var actions = new List<DisplayAction>();
        var lastTime = events.Count == 0 ? 0 : events[^1].T;

        PendingMouse? mouse = null;
        var modifiersDown = new Dictionary<int, long>();
        var modifiersUsed = new HashSet<int>();
        var pendingKeys = new Dictionary<int, PendingKey>();

        foreach (var evt in events)
        {
            switch (evt.Kind)
            {
                case MacroEventKind.Move:
                {
                    if (mouse != null)
                    {
                        mouse.PathLength += Distance(mouse.LastX, mouse.LastY, evt.X, evt.Y);
                        mouse.LastX = evt.X;
                        mouse.LastY = evt.Y;
                        mouse.MoveCount++;
                    }

                    break;
                }
                case MacroEventKind.Down:
                {
                    if (mouse != null)
                    {
                        CompleteMouse(actions, mouse, evt.T);
                    }

                    mouse = new PendingMouse
                    {
                        Button = (MouseButtonKind)evt.Data,
                        DownAt = evt.T,
                        StartX = evt.X,
                        StartY = evt.Y,
                        LastX = evt.X,
                        LastY = evt.Y
                    };
                    break;
                }
                case MacroEventKind.Up:
                {
                    if (mouse != null && (int)mouse.Button == evt.Data)
                    {
                        mouse.PathLength += Distance(mouse.LastX, mouse.LastY, evt.X, evt.Y);
                        mouse.LastX = evt.X;
                        mouse.LastY = evt.Y;
                        CompleteMouse(actions, mouse, evt.T, evt.X, evt.Y);
                        mouse = null;
                    }
                    else
                    {
                        actions.Add(new DisplayAction(
                            evt.T,
                            0,
                            DisplayActionKind.Click,
                            $"{KeyNames.MouseName((MouseButtonKind)evt.Data)}释放",
                            $"坐标 ({evt.X}, {evt.Y})",
                            1,
                            evt.X,
                            evt.Y));
                    }

                    break;
                }
                case MacroEventKind.Wheel:
                {
                    var horizontal = evt.Horizontal;
                    var direction = evt.Data > 0
                        ? (horizontal ? "向右" : "向上")
                        : (horizontal ? "向左" : "向下");
                    var notches = Math.Max(1, Math.Abs(evt.Data) / 120);
                    var title = $"滚轮{direction}";

                    if (actions.Count > 0)
                    {
                        var previous = actions[^1];
                        if (previous.Kind == DisplayActionKind.Scroll &&
                            previous.Title == title &&
                            previous.Horizontal == horizontal &&
                            evt.T - (previous.StartMs + previous.DurationMs) <= 220 &&
                            Math.Abs(evt.X - previous.X) < 40 &&
                            Math.Abs(evt.Y - previous.Y) < 40)
                        {
                            actions[^1] = previous with
                            {
                                DurationMs = evt.T - previous.StartMs,
                                Detail = $"{evt.X}, {evt.Y} · {(horizontal ? "横向" : "竖向")}",
                                Count = previous.Count + notches,
                                X = evt.X,
                                Y = evt.Y
                            };
                            break;
                        }
                    }

                    actions.Add(new DisplayAction(
                        evt.T,
                        0,
                        DisplayActionKind.Scroll,
                        title,
                        $"{evt.X}, {evt.Y} · {(horizontal ? "横向" : "竖向")}",
                        notches,
                        evt.X,
                        evt.Y,
                        horizontal));
                    break;
                }
                case MacroEventKind.KeyDown:
                {
                    var vk = evt.Data;
                    if (KeyNames.IsModifier(vk))
                    {
                        if (!modifiersDown.ContainsKey(vk))
                        {
                            modifiersDown[vk] = evt.T;
                        }

                        modifiersUsed.Remove(vk);
                        break;
                    }

                    if (pendingKeys.ContainsKey(vk))
                    {
                        break;
                    }

                    var comboParts = new List<string>();
                    foreach (var mod in OrderedModifiers(modifiersDown.Keys))
                    {
                        comboParts.Add(KeyNames.GetName(mod));
                        modifiersUsed.Add(mod);
                    }

                    comboParts.Add(KeyNames.GetName(vk));
                    var combo = string.Join(" + ", comboParts);
                    var action = new DisplayAction(
                        evt.T,
                        0,
                        DisplayActionKind.Key,
                        combo,
                        comboParts.Count > 1 ? "组合键" : "键盘按键");
                    actions.Add(action);
                    pendingKeys[vk] = new PendingKey(action, evt.T);
                    break;
                }
                case MacroEventKind.KeyUp:
                {
                    var vk = evt.Data;
                    if (KeyNames.IsModifier(vk))
                    {
                        var downAt = modifiersDown.TryGetValue(vk, out var t) ? t : evt.T;
                        var wasUsed = modifiersUsed.Contains(vk);
                        modifiersDown.Remove(vk);
                        modifiersUsed.Remove(vk);

                        if (!wasUsed && modifiersDown.Count == 0)
                        {
                            // A modifier tapped on its own is still a meaningful action.
                            actions.Add(new DisplayAction(
                                downAt,
                                0,
                                DisplayActionKind.Key,
                                KeyNames.GetName(vk),
                                "键盘按键"));
                        }

                        break;
                    }

                    if (pendingKeys.TryGetValue(vk, out var pending))
                    {
                        var index = actions.IndexOf(pending.Action);
                        if (index >= 0)
                        {
                            var duration = Math.Max(0, evt.T - pending.StartMs);
                            var isCombo = pending.Action.Title.Contains(" + ");
                            actions[index] = pending.Action with
                            {
                                DurationMs = duration,
                                Detail = (isCombo ? "组合键" : "键盘按键") + $" · {duration} ms"
                            };
                        }

                        pendingKeys.Remove(vk);
                    }

                    break;
                }
            }
        }

        if (includePending)
        {
            if (mouse != null)
            {
                CompleteMouse(actions, mouse, lastTime);
            }

            foreach (var pending in pendingKeys.Values)
            {
                var index = actions.IndexOf(pending.Action);
                if (index >= 0)
                {
                    var duration = Math.Max(0, lastTime - pending.StartMs);
                    actions[index] = pending.Action with
                    {
                        DurationMs = duration,
                        Detail = "按键未松开 · 录制结束时仍按住"
                    };
                }
            }
        }

        actions.Sort((a, b) =>
        {
            var c = a.StartMs.CompareTo(b.StartMs);
            return c != 0 ? c : a.Kind.CompareTo(b.Kind);
        });

        return new DisplayBuildResult
        {
            Actions = actions,
            Summary = new MacroSummary
            {
                ActionCount = actions.Count,
                ClickCount = actions.Count(a => a.Kind == DisplayActionKind.Click),
                DragCount = actions.Count(a => a.Kind == DisplayActionKind.Drag),
                KeyCount = actions.Count(a => a.Kind == DisplayActionKind.Key),
                ScrollCount = actions.Count(a => a.Kind == DisplayActionKind.Scroll),
                DurationMs = lastTime
            }
        };
    }

    private static void CompleteMouse(
        List<DisplayAction> actions,
        PendingMouse mouse,
        long endTime,
        int? endX = null,
        int? endY = null)
    {
        var ex = endX ?? mouse.LastX;
        var ey = endY ?? mouse.LastY;
        var duration = Math.Max(0, endTime - mouse.DownAt);
        var straight = Distance(mouse.StartX, mouse.StartY, ex, ey);

        if (mouse.PathLength >= 5 || straight >= 5 || mouse.MoveCount > 2)
        {
            var distance = Math.Max((int)Math.Round(straight), (int)Math.Round(mouse.PathLength));
            actions.Add(new DisplayAction(
                mouse.DownAt,
                duration,
                DisplayActionKind.Drag,
                $"{KeyNames.MouseName(mouse.Button)}拖动",
                $"({mouse.StartX}, {mouse.StartY}) → ({ex}, {ey}) · {distance}px",
                1,
                ex,
                ey));
        }
        else
        {
            var title = mouse.Button == MouseButtonKind.Left
                ? "左键单击"
                : KeyNames.MouseVerb(mouse.Button);
            actions.Add(new DisplayAction(
                mouse.DownAt,
                duration,
                DisplayActionKind.Click,
                title,
                $"坐标 ({ex}, {ey})",
                1,
                ex,
                ey));
        }
    }

    private static IEnumerable<int> OrderedModifiers(IEnumerable<int> modifiers)
    {
        var set = modifiers.ToHashSet();
        if (set.Contains(KeyNames.VkControl))
        {
            yield return KeyNames.VkControl;
        }

        if (set.Contains(KeyNames.VkShift))
        {
            yield return KeyNames.VkShift;
        }

        if (set.Contains(KeyNames.VkAlt))
        {
            yield return KeyNames.VkAlt;
        }

        if (set.Contains(KeyNames.VkLWin) || set.Contains(KeyNames.VkRWin))
        {
            yield return KeyNames.VkLWin;
        }
    }

    private static double Distance(int x1, int y1, int x2, int y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private sealed class PendingMouse
    {
        public MouseButtonKind Button { get; init; }
        public long DownAt { get; init; }
        public int StartX { get; init; }
        public int StartY { get; init; }
        public int LastX { get; set; }
        public int LastY { get; set; }
        public double PathLength { get; set; }
        public int MoveCount { get; set; }
    }

    private sealed record PendingKey(DisplayAction Action, long StartMs);
}
