using System.Drawing.Drawing2D;
using ClickFlow.Core;

namespace ClickFlow.UI.Controls;

internal class TimelineView : Control
{
    private const int RowHeight = 56;
    private const int TopPadding = 8;
    private const int BottomPadding = 8;
    private IReadOnlyList<DisplayAction> _actions = Array.Empty<DisplayAction>();
    private float _scroll;
    private int _hoverIndex = -1;
    private int _selectedIndex = -1;
    private bool _draggingThumb;
    private float _dragOffset;
    private bool _isRecording;
    private float _pulsePhase;

    public bool IsRecording
    {
        get => _isRecording;
        set { _isRecording = value; Invalidate(); }
    }

    public float PulsePhase
    {
        get => _pulsePhase;
        set { _pulsePhase = value; Invalidate(); }
    }

    public IReadOnlyList<DisplayAction> Actions => _actions;

    public TimelineView()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        BackColor = Color.Transparent;
        TabStop = true;
        Cursor = Cursors.Default;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    public void SetActions(IReadOnlyList<DisplayAction> actions, bool scrollToEnd = false)
    {
        _actions = actions ?? Array.Empty<DisplayAction>();
        _selectedIndex = Math.Clamp(_selectedIndex, -1, _actions.Count - 1);
        _hoverIndex = -1;
        _scroll = Math.Clamp(_scroll, 0, MaxScroll);
        if (scrollToEnd)
        {
            _scroll = MaxScroll;
        }

        Invalidate();
    }

    public void ScrollToEnd()
    {
        _scroll = MaxScroll;
        Invalidate();
    }

    private float ContentHeight => _actions.Count * (float)RowHeight + TopPadding + BottomPadding;

    private float MaxScroll => Math.Max(0, ContentHeight - Height);

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (_actions.Count == 0)
        {
            DrawEmpty(e.Graphics);
            return;
        }

        var viewport = new RectangleF(0, 0, Width - (MaxScroll > 0 ? 11 : 4), Height);
        var first = Math.Max(0, (int)((_scroll - TopPadding) / RowHeight));
        var last = Math.Min(_actions.Count - 1, first + (int)Math.Ceiling(Height / (double)RowHeight) + 1);

        var lineTop = Math.Max(6f, TopPadding + first * RowHeight - _scroll + RowHeight / 2f);
        var lineBottom = Math.Min(Height - 8f, TopPadding + last * RowHeight - _scroll + RowHeight / 2f);
        if (last > first)
        {
            using var linePen = new Pen(Theme.BorderSoft, 1.2f);
            e.Graphics.DrawLine(linePen, 31, lineTop, 31, lineBottom);
        }

        for (var i = first; i <= last; i++)
        {
            var y = TopPadding + i * RowHeight - _scroll;
            var rowRect = new RectangleF(7, y + 3, Math.Max(10, viewport.Width - 14), RowHeight - 6);
            if (i == _hoverIndex)
            {
                Theme.FillRounded(e.Graphics, rowRect, 11, Theme.Alpha(Theme.SurfaceHover, 135));
            }
            else if (i == _selectedIndex)
            {
                Theme.FillRounded(e.Graphics, rowRect, 11, Theme.Alpha(Theme.Blue, 20));
            }

            DrawRow(e.Graphics, _actions[i], y, (int)viewport.Width);
        }

        if (MaxScroll > 0)
        {
            DrawScrollbar(e.Graphics);
        }
    }

    private void DrawRow(Graphics g, DisplayAction action, float y, int width)
    {
        var (color, glyph) = action.Kind switch
        {
            DisplayActionKind.Click => (Theme.Accent, Glyph.Mouse),
            DisplayActionKind.Drag => (Theme.Purple, Glyph.Drag),
            DisplayActionKind.Scroll => (Theme.Amber, Glyph.Scroll),
            _ => (Theme.Blue, Glyph.Keyboard)
        };

        var iconRect = new RectangleF(16, y + 13, 30, 30);
        using (var iconBrush = new SolidBrush(Theme.Alpha(color, 24)))
        {
            g.FillEllipse(iconBrush, iconRect);
        }

        using (var pen = new Pen(Theme.Alpha(color, 60), 1f))
        {
            g.DrawEllipse(pen, iconRect);
        }

        Theme.DrawGlyph(
            g,
            glyph,
            new RectangleF(iconRect.Left + 8, iconRect.Top + 8, 14, 14),
            color,
            1.6f);

        var titleRect = new Rectangle(
            56,
            (int)y + 7,
            Math.Max(10, width - 56 - 88),
            23);
        TextRenderer.DrawText(
            g,
            action.Title,
            Theme.LabelBold,
            titleRect,
            Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        var titleWidth = TextRenderer.MeasureText(action.Title, Theme.LabelBold).Width;
        if (action.Count > 1 && titleWidth < titleRect.Width - 30)
        {
            var badge = new RectangleF(
                titleRect.Left + titleWidth + 8,
                y + 10,
                28,
                17);
            Theme.FillRounded(g, badge, 5, Theme.Alpha(color, 25));
            TextRenderer.DrawText(
                g,
                "×" + action.Count,
                Theme.TinyMono,
                badge.ToRectangle(),
                color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        TextRenderer.DrawText(
            g,
            action.Detail,
            Theme.BodySmall,
            new Rectangle(56, (int)y + 29, Math.Max(10, width - 56 - 88), 19),
            Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            g,
            Theme.FormatDuration(action.StartMs),
            Theme.Mono,
            new Rectangle(width - 83, (int)y + 8, 70, 19),
            Theme.TextSoft,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        if (action.DurationMs > 0)
        {
            TextRenderer.DrawText(
                g,
                $"{action.DurationMs} ms",
                Theme.TinyMono,
                new Rectangle(width - 83, (int)y + 28, 70, 18),
                Theme.MutedDim,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }

    private void DrawEmpty(Graphics g)
    {
        var centerX = Width / 2f;
        var centerY = Math.Max(110, Height * 0.36f);
        var accent = _isRecording ? Theme.Accent : Theme.Blue;

        for (var i = 2; i >= 0; i--)
        {
            var radius = 52 + i * 22;
            using var ring = new Pen(Theme.Alpha(accent, i == 0 ? 28 : 12), 1f);
            g.DrawEllipse(ring, centerX - radius, centerY - radius, radius * 2, radius * 2);
        }

        var circle = new RectangleF(centerX - 30, centerY - 30, 60, 60);
        using (var circleBrush = new SolidBrush(Theme.Alpha(accent, 22)))
        {
            g.FillEllipse(circleBrush, circle);
        }

        using (var pen = new Pen(Theme.Alpha(accent, 65), 1.2f))
        {
            g.DrawEllipse(pen, circle);
        }

        Theme.DrawGlyph(
            g,
            _isRecording ? Glyph.Record : Glyph.Mouse,
            new RectangleF(centerX - 14, centerY - 14, 28, 28),
            accent,
            1.8f);

        if (_isRecording)
        {
            var pulse = 4 + Math.Clamp(_pulsePhase, 0, 1) * 6;
            using var dot = new SolidBrush(Theme.Accent);
            g.FillEllipse(dot, centerX - 3, centerY + 56 - 3, 6, 6);
            using var pen = new Pen(Theme.Alpha(Theme.Accent, 35), 1f);
            g.DrawEllipse(pen, centerX - 3 - pulse, centerY + 56 - 3 - pulse, 6 + pulse * 2, 6 + pulse * 2);
        }

        var title = _isRecording ? "正在录制，操作会出现在这里" : "还没有操作";
        var subtitle = _isRecording
            ? "点击、拖动、滚动或按键，按 F1 结束录制"
            : "按下 F1 开始录制，或打开一个已有的宏文件";

        TextRenderer.DrawText(
            g,
            title,
            Theme.Heading,
            new Rectangle(20, (int)(centerY + 82), Width - 40, 30),
            _isRecording ? Theme.AccentSoft : Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            g,
            subtitle,
            Theme.Body,
            new Rectangle(24, (int)(centerY + 113), Width - 48, 26),
            Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        if (!_isRecording)
        {
            var hint = new RectangleF(centerX - 82, centerY + 153, 164, 28);
            Theme.FillRounded(g, hint, 8, Theme.Alpha(Theme.SurfaceHover, 130));
            Theme.DrawRounded(g, hint, 8, Theme.BorderSoft);
            TextRenderer.DrawText(
                g,
                "F1  开始录制",
                Theme.BodySmall,
                hint.ToRectangle(),
                Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }

    private void DrawScrollbar(Graphics g)
    {
        var track = TrackRect();
        Theme.FillRounded(g, track, 3, Theme.Alpha(Theme.SurfaceHover, 90));
        var thumb = ThumbRect();
        Theme.FillRounded(g, thumb, 3, _draggingThumb ? Theme.Muted : Theme.Border);
    }

    private RectangleF TrackRect()
    {
        return new RectangleF(Math.Max(1, Width - 8), 8, 4, Math.Max(1, Height - 16));
    }

    private RectangleF ThumbRect()
    {
        var track = TrackRect();
        var content = Math.Max(ContentHeight, Height);
        var thumbHeight = Math.Max(28f, track.Height * (Height / content));
        var progress = MaxScroll <= 0 ? 0 : _scroll / MaxScroll;
        var top = track.Top + (track.Height - thumbHeight) * progress;
        return new RectangleF(track.Left, top, track.Width, thumbHeight);
    }

    private int RowAt(Point point)
    {
        if (point.X > Width - 14)
        {
            return -1;
        }

        var index = (int)Math.Floor((point.Y + _scroll - TopPadding) / RowHeight);
        return index >= 0 && index < _actions.Count ? index : -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_draggingThumb)
        {
            var track = TrackRect();
            var thumbHeight = ThumbRect().Height;
            var available = Math.Max(1, track.Height - thumbHeight);
            var top = Math.Clamp(e.Y - _dragOffset, track.Top, track.Bottom - thumbHeight);
            _scroll = (top - track.Top) / available * MaxScroll;
            Invalidate();
            return;
        }

        var index = RowAt(e.Location);
        if (index != _hoverIndex)
        {
            _hoverIndex = index;
            Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (!_draggingThumb)
        {
            _hoverIndex = -1;
            Invalidate();
        }

        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        if (e.Button == MouseButtons.Left && MaxScroll > 0 && ThumbRect().Contains(e.Location))
        {
            _draggingThumb = true;
            _dragOffset = e.Y - ThumbRect().Top;
            Invalidate();
        }
        else if (e.Button == MouseButtons.Left)
        {
            _selectedIndex = RowAt(e.Location);
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _draggingThumb = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (MaxScroll <= 0)
        {
            return;
        }

        var lines = Math.Max(1, SystemInformation.MouseWheelScrollLines);
        var delta = (e.Delta / 120f) * lines * (RowHeight * 0.6f);
        _scroll = Math.Clamp(_scroll - delta, 0, MaxScroll);
        Invalidate();
        base.OnMouseWheel(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        Focus();
        base.OnMouseEnter(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End ||
               base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var page = Math.Max(RowHeight, Height - RowHeight);
        switch (e.KeyCode)
        {
            case Keys.PageUp:
                _scroll = Math.Clamp(_scroll - page, 0, MaxScroll);
                e.Handled = true;
                break;
            case Keys.PageDown:
                _scroll = Math.Clamp(_scroll + page, 0, MaxScroll);
                e.Handled = true;
                break;
            case Keys.Home:
                _scroll = 0;
                e.Handled = true;
                break;
            case Keys.End:
                _scroll = MaxScroll;
                e.Handled = true;
                break;
        }

        if (e.Handled)
        {
            Invalidate();
        }

        base.OnKeyDown(e);
    }
}
