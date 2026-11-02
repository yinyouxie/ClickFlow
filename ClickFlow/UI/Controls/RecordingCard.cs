using System.Drawing.Drawing2D;
using ClickFlow.Core;
using ClickFlow.UI;

namespace ClickFlow.UI.Controls;

internal sealed class RecordingCard : Control
{
    private const int MaxPreviewActions = 40;

    private readonly float _scale;
    private readonly int _headerHeight;
    private readonly int _rowHeight;
    private List<DisplayAction>? _actions;
    private string? _loadError;
    private bool _selected;
    private bool _expanded;
    private bool _hovered;
    private Rectangle _playRect;
    private Rectangle _expandRect;
    private Rectangle _deleteRect;

    public RecordingEntry Entry { get; }

    public bool IsSelected => _selected;

    public bool IsExpanded => _expanded;

    public event Action<RecordingCard>? SelectedRequested;

    public event Action<RecordingCard>? PlayRequested;

    public event Action<RecordingCard>? DeleteRequested;

    public event Action<RecordingCard>? ExpandedChanged;

    public RecordingCard(RecordingEntry entry, float scale = 1f)
    {
        Entry = entry;
        _scale = Math.Max(1f, scale);
        _headerHeight = S(84);
        _rowHeight = S(38);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
        Height = _headerHeight;
        Margin = new Padding(0, 0, 0, S(8));
        AccessibleRole = AccessibleRole.ListItem;
        AccessibleName = entry.Name;
        LayoutRects();
    }

    public void SetSelected(bool selected)
    {
        if (_selected == selected)
        {
            return;
        }

        _selected = selected;
        Invalidate();
    }

    public void SetExpanded(bool expanded)
    {
        if (_expanded == expanded)
        {
            return;
        }

        _expanded = expanded;
        if (expanded)
        {
            LoadActions();
        }
        else
        {
            _actions = null;
            _loadError = null;
        }

        UpdateHeight();
        Invalidate();
        ExpandedChanged?.Invoke(this);
    }

    public void ToggleExpanded()
    {
        SetExpanded(!_expanded);
    }

    public void UpdateHeight()
    {
        var height = _headerHeight;
        if (_expanded)
        {
            var rows = _actions?.Count ?? 0;
            if (rows > MaxPreviewActions)
            {
                rows = MaxPreviewActions;
            }

            if (rows == 0)
            {
                rows = 1;
            }

            height += S(10) + rows * _rowHeight + S(12);
            if ((_actions?.Count ?? 0) > MaxPreviewActions)
            {
                height += S(24);
            }
        }

        Height = height;
    }

    private int S(float value) => (int)Math.Round(value * _scale);

    private void LoadActions()
    {
        try
        {
            var document = RecordingLibrary.LoadDocument(Entry);
            _actions = DisplayActionBuilder.Build(document.Events, includePending: true).Actions;
            _loadError = null;
        }
        catch (Exception ex)
        {
            _actions = null;
            _loadError = ex.Message;
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0, 0, Width, Height);
        var fill = _selected
            ? Theme.Blend(Theme.Surface, Theme.Blue, 0.09)
            : _hovered
                ? Theme.SurfaceRaised
                : Theme.Surface;

        Theme.FillRounded(e.Graphics, rect, S(14), fill);
        Theme.DrawRounded(
            e.Graphics,
            rect,
            S(14),
            _selected ? Theme.Blue : Theme.BorderSoft,
            _selected ? 1.7f : 1f);

        DrawHeader(e.Graphics);
        if (_expanded)
        {
            DrawActions(e.Graphics);
        }
    }

    private void DrawHeader(Graphics g)
    {
        var iconRect = new RectangleF(S(16), S(22), S(40), S(40));
        var iconColor = _selected ? Theme.Blue : Theme.Muted;
        using (var brush = new SolidBrush(Theme.Alpha(iconColor, 22)))
        {
            g.FillEllipse(brush, iconRect);
        }

        using (var pen = new Pen(Theme.Alpha(iconColor, 70), 1f))
        {
            g.DrawEllipse(pen, iconRect);
        }

        Theme.DrawGlyph(
            g,
            Glyph.Mouse,
            new RectangleF(iconRect.Left + S(11), iconRect.Top + S(10), S(18), S(20)),
            iconColor,
            1.6f);

        var buttonsLeft = Width - S(122);
        var titleWidth = Math.Max(S(30), buttonsLeft - S(70));
        TextRenderer.DrawText(
            g,
            Entry.Name,
            Theme.LabelBold,
            new Rectangle(S(70), S(11), titleWidth, S(24)),
            Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        var meta = L.T(
            $"{Entry.CreatedLocal:MM-dd HH:mm:ss}  ·  {RecordingEntry.FormatDuration(Entry.DurationMs)}  ·  {Entry.ActionCount} 个操作",
            $"{Entry.CreatedLocal:MM-dd HH:mm:ss}  ·  {RecordingEntry.FormatDuration(Entry.DurationMs)}  ·  {Entry.ActionCount} actions");
        TextRenderer.DrawText(
            g,
            meta,
            Theme.BodySmall,
            new Rectangle(S(70), S(35), titleWidth, S(20)),
            Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            g,
            Entry.DetailSummary,
            Theme.BodySmall,
            new Rectangle(S(70), S(55), titleWidth, S(18)),
            Theme.MutedDim,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        DrawPlayButton(g);
        DrawButton(g, _expandRect, _expanded ? Glyph.ChevronUp : Glyph.ChevronDown, Theme.Muted);
        DrawButton(g, _deleteRect, Glyph.Clear, Theme.MutedDim, dangerOnHover: true);
    }

    private void DrawPlayButton(Graphics g)
    {
        var active = _selected;
        var accent = active ? Theme.Green : Theme.MutedDim;
        var fill = active ? Theme.Alpha(Theme.Green, 34) : Theme.Alpha(Theme.SurfaceHover, 120);
        Theme.FillRounded(g, _playRect, _playRect.Height / 2f, fill);
        Theme.DrawRounded(g, _playRect, _playRect.Height / 2f, Theme.Alpha(accent, active ? 110 : 45));
        Theme.DrawGlyph(
            g,
            Glyph.Play,
            new RectangleF(_playRect.Left + S(10), _playRect.Top + S(8), S(14), S(16)),
            accent,
            1.6f);
    }

    private void DrawButton(Graphics g, Rectangle rect, Glyph glyph, Color color, bool dangerOnHover = false)
    {
        var hovered = rect.Contains(PointToClient(Cursor.Position));
        if (hovered)
        {
            Theme.FillRounded(g, rect, S(7), Theme.Alpha(Theme.SurfaceHover, 190));
        }

        var actual = hovered && dangerOnHover ? Theme.Accent : color;
        Theme.DrawGlyph(
            g,
            glyph,
            new RectangleF(rect.Left + S(6), rect.Top + S(6), S(14), S(14)),
            actual,
            1.5f);
    }

    private void DrawActions(Graphics g)
    {
        var y = _headerHeight;
        using (var pen = new Pen(Theme.BorderSoft, 1f))
        {
            g.DrawLine(pen, S(16), y + 0.5f, Width - S(16), y + 0.5f);
        }

        y += S(10);
        if (!string.IsNullOrWhiteSpace(_loadError))
        {
            TextRenderer.DrawText(
                g,
                L.T("无法读取录制文件：", "Cannot read recording: ") + _loadError,
                Theme.BodySmall,
                new Rectangle(S(24), y + S(8), Width - S(48), S(22)),
                Theme.Accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);
            return;
        }

        if (_actions == null || _actions.Count == 0)
        {
            TextRenderer.DrawText(
                g,
                L.T("这条录制只有鼠标移动轨迹", "This recording only contains mouse movement"),
                Theme.BodySmall,
                new Rectangle(S(24), y + S(8), Width - S(48), S(22)),
                Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            return;
        }

        var count = Math.Min(_actions.Count, MaxPreviewActions);
        for (var i = 0; i < count; i++)
        {
            var action = _actions[i];
            DrawActionRow(g, action, y);
            y += _rowHeight;
        }

        if (_actions.Count > MaxPreviewActions)
        {
            TextRenderer.DrawText(
                g,
                L.T(
                    $"还有 {_actions.Count - MaxPreviewActions} 个操作未展开显示",
                    $"{_actions.Count - MaxPreviewActions} more actions not shown"),
                Theme.BodySmall,
                new Rectangle(S(24), y + S(4), Width - S(48), S(20)),
                Theme.MutedDim,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }

    private void DrawActionRow(Graphics g, DisplayAction action, int y)
    {
        var (color, glyph) = action.Kind switch
        {
            DisplayActionKind.Click => (Theme.Accent, Glyph.Mouse),
            DisplayActionKind.Drag => (Theme.Purple, Glyph.Drag),
            DisplayActionKind.Scroll => (Theme.Amber, Glyph.Scroll),
            _ => (Theme.Blue, Glyph.Keyboard)
        };

        var icon = new RectangleF(S(24), y + S(10), S(18), S(18));
        using (var brush = new SolidBrush(Theme.Alpha(color, 20)))
        {
            g.FillEllipse(brush, icon);
        }

        Theme.DrawGlyph(
            g,
            glyph,
            new RectangleF(icon.Left + S(3), icon.Top + S(3), S(12), S(12)),
            color,
            1.4f);

        TextRenderer.DrawText(
            g,
            action.Title,
            Theme.BodyBold,
            new Rectangle(S(52), y + S(2), Math.Max(S(20), Width - S(52) - S(100)), S(20)),
            Theme.TextSoft,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            g,
            action.Detail,
            Theme.BodySmall,
            new Rectangle(S(52), y + S(19), Math.Max(S(20), Width - S(52) - S(100)), S(17)),
            Theme.MutedDim,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            g,
            Theme.FormatDuration(action.StartMs),
            Theme.TinyMono,
            new Rectangle(Width - S(92), y + S(9), S(72), S(20)),
            Theme.Muted,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutRects();
    }

    private void LayoutRects()
    {
        _deleteRect = new Rectangle(Width - S(38), S(28), S(26), S(26));
        _expandRect = new Rectangle(Width - S(70), S(28), S(26), S(26));
        _playRect = new Rectangle(Width - S(116), S(24), S(38), S(38));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var wasHovered = _hovered;
        _hovered = ClientRectangle.Contains(e.Location);
        Cursor = _playRect.Contains(e.Location) ||
                 _expandRect.Contains(e.Location) ||
                 _deleteRect.Contains(e.Location)
            ? Cursors.Hand
            : Cursors.Hand;
        if (wasHovered != _hovered)
        {
            Invalidate();
        }
        else if (_playRect.Contains(e.Location) || _expandRect.Contains(e.Location) || _deleteRect.Contains(e.Location))
        {
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_playRect.Contains(e.Location))
            {
                PlayRequested?.Invoke(this);
            }
            else if (_expandRect.Contains(e.Location))
            {
                SelectedRequested?.Invoke(this);
                SetExpanded(!_expanded);
            }
            else if (_deleteRect.Contains(e.Location))
            {
                DeleteRequested?.Invoke(this);
            }
            else
            {
                SelectedRequested?.Invoke(this);
            }
        }

        base.OnMouseUp(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            SelectedRequested?.Invoke(this);
            SetExpanded(!_expanded);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _actions = null;
        }

        base.Dispose(disposing);
    }
}
