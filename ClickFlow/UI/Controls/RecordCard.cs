using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal class RecordCard : Control
{
    private bool _recording;
    private bool _hovered;
    private float _pulsePhase;
    private string _hint = L.T("F1 开始 / 停止", "F1 start / stop");
    private string _hotkeyLabel = "F1";

    public string HotkeyLabel
    {
        get => _hotkeyLabel;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? "—" : value;
            if (_hotkeyLabel == next)
            {
                return;
            }

            _hotkeyLabel = next;
            Invalidate();
        }
    }

    public bool IsRecording
    {
        get => _recording;
        set { _recording = value; Invalidate(); }
    }

    public float PulsePhase
    {
        get => _pulsePhase;
        set { _pulsePhase = value; Invalidate(); }
    }

    public string Hint
    {
        get => _hint;
        set { _hint = value; Invalidate(); }
    }

    public RecordCard()
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
        Cursor = Cursors.Hand;
        TabStop = true;
        Height = 108;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = L.T("录制操作", "Recording");
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0, 0, Width, Height);

        var top = _recording
            ? Theme.Blend(Theme.Surface, Theme.Accent, 0.12)
            : _hovered
                ? Theme.SurfaceHover
                : Theme.SurfaceRaised;
        var bottom = _recording
            ? Theme.Blend(Theme.Bg, Theme.Accent, 0.08)
            : Theme.Surface;
        using (var gradient = new LinearGradientBrush(rect, top, bottom, 28f))
        {
            Theme.FillRounded(e.Graphics, rect, 16, gradient);
        }

        Theme.DrawRounded(
            e.Graphics,
            rect,
            16,
            _recording
                ? Theme.Alpha(Theme.Accent, 120)
                : _hovered
                    ? Theme.Alpha(Theme.Accent, 55)
                    : Theme.Border,
            _recording ? 1.4f : 1f);

        if (_recording)
        {
            var glowAlpha = (int)(38 * (1 - Math.Clamp(_pulsePhase, 0, 1)));
            using var glow = new Pen(Theme.Alpha(Theme.Accent, Math.Max(0, glowAlpha)), 1.2f);
            e.Graphics.DrawEllipse(
                glow,
                24 - 5 - _pulsePhase * 7,
                Height / 2f - 25 - 5 - _pulsePhase * 7,
                60 + _pulsePhase * 14,
                60 + _pulsePhase * 14);
        }

        var circle = new RectangleF(20, (Height - 50) / 2f, 50, 50);
        using (var circleBrush = new SolidBrush(Theme.Alpha(Theme.Accent, 24)))
        {
            e.Graphics.FillEllipse(circleBrush, circle);
        }

        Theme.DrawRounded(e.Graphics, circle, 25, Theme.Alpha(Theme.Accent, _recording ? 110 : 70));

        if (_recording)
        {
            Theme.FillRounded(e.Graphics, new RectangleF(circle.Left + 16, circle.Top + 16, 18, 18), 4, Theme.Accent);
        }
        else
        {
            using var dot = new SolidBrush(Theme.Accent);
            e.Graphics.FillEllipse(dot, circle.Left + 12, circle.Top + 12, 26, 26);
        }

        var title = _recording
            ? L.T("停止录制", "Stop recording")
            : L.T("录制操作", "Record");
        var subtitle = _recording
            ? L.T(
                $"正在捕获鼠标与键盘，按 {_hotkeyLabel} 结束",
                $"Capturing mouse and keyboard; press {_hotkeyLabel} to stop")
            : L.T("点击 · 拖动 / 滑动 · 滚轮 · 键盘", "Click · drag / swipe · wheel · keyboard");

        TextRenderer.DrawText(
            e.Graphics,
            title,
            Theme.Heading,
            new Rectangle(84, 25, Width - 112, 28),
            _recording ? Theme.AccentSoft : Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            e.Graphics,
            subtitle,
            Theme.BodySmall,
            new Rectangle(85, 53, Width - 100, 24),
            Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        var chipScale = WindowEffects.GetDpiScale(this);
        var chipText = _recording
            ? L.T($"{_hotkeyLabel} 停止", $"{_hotkeyLabel} stop")
            : _hotkeyLabel;
        var chipWidth = Math.Max(
            40f * chipScale,
            TextRenderer.MeasureText(chipText, Theme.BodySmall).Width + 16f * chipScale);
        var chipHeight = 22f * chipScale;
        var chip = new RectangleF(
            Width - chipWidth - 16f * chipScale,
            13f * chipScale,
            chipWidth,
            chipHeight);
        Theme.FillRounded(e.Graphics, chip, 7, Theme.Alpha(Theme.SurfaceHover, 215));
        Theme.DrawRounded(e.Graphics, chip, 7, Theme.Alpha(Theme.Accent, 55));
        TextRenderer.DrawText(
            e.Graphics,
            chipText,
            Theme.BodySmall,
            chip.ToRectangle(),
            _recording ? Theme.AccentSoft : Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            e.Graphics,
            _hint,
            Theme.BodySmall,
            new Rectangle(85, Height - 31, Width - 102, 18),
            Theme.MutedDim,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Focus();
            Invalidate();
        }

        base.OnMouseDown(e);
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

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.Space or Keys.Enter || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            e.Handled = true;
            OnClick(EventArgs.Empty);
        }

        base.OnKeyDown(e);
    }
}
