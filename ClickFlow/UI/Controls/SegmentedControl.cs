using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal class SegmentedControl : Control
{
    private string[] _items = Array.Empty<string>();
    private int _selectedIndex;
    private int _hoverIndex = -1;
    private Color _accentColor = Theme.Blue;
    private Color _selectedTextColor = Theme.Text;
    private bool _showAccentBar;

    public string[] Items
    {
        get => _items;
        set
        {
            _items = value ?? Array.Empty<string>();
            _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, _items.Length - 1));
            Invalidate();
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var next = _items.Length == 0 ? 0 : Math.Clamp(value, 0, _items.Length - 1);
            if (_selectedIndex == next)
            {
                return;
            }

            _selectedIndex = next;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    public Color SelectedTextColor
    {
        get => _selectedTextColor;
        set { _selectedTextColor = value; Invalidate(); }
    }

    public bool ShowAccentBar
    {
        get => _showAccentBar;
        set { _showAccentBar = value; Invalidate(); }
    }

    public event EventHandler? SelectedIndexChanged;

    public SegmentedControl()
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
        Font = Theme.BodyBold;
        Height = 36;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PageTabList;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var enabled = Enabled;
        var bounds = new RectangleF(0, 0, Width, Height);
        Theme.FillRounded(
            e.Graphics,
            bounds,
            10,
            enabled ? Theme.Field : Theme.Blend(Theme.Field, Theme.Bg, 0.55));
        Theme.DrawRounded(e.Graphics, bounds, 10, Theme.BorderSoft);

        if (Items.Length == 0)
        {
            return;
        }

        var inset = 3f;
        var segmentWidth = (Width - inset * 2) / Items.Length;
        for (var i = 0; i < Items.Length; i++)
        {
            var rect = new RectangleF(
                inset + i * segmentWidth,
                inset,
                segmentWidth,
                Height - inset * 2);

            if (i == _selectedIndex)
            {
                Theme.FillRounded(
                    e.Graphics,
                    rect,
                    8,
                    Theme.Alpha(AccentColor, enabled ? 24 : 12));
                Theme.DrawRounded(
                    e.Graphics,
                    rect,
                    8,
                    Theme.Alpha(AccentColor, enabled ? 58 : 28));

                if (ShowAccentBar)
                {
                    var bar = new RectangleF(rect.Left + 12, rect.Bottom - 2.5f, rect.Width - 24, 2f);
                    Theme.FillRounded(e.Graphics, bar, 1f, enabled ? AccentColor : Theme.MutedDim);
                }
            }
            else if (enabled && i == _hoverIndex)
            {
                Theme.FillRounded(e.Graphics, rect, 8, Theme.Alpha(Theme.SurfaceHover, 110));
            }

            var color = !enabled
                ? Theme.MutedDim
                : i == _selectedIndex
                    ? SelectedTextColor
                    : Theme.Muted;
            TextRenderer.DrawText(
                e.Graphics,
                Items[i],
                Font,
                rect.ToRectangle(),
                color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!Enabled)
        {
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }

            return;
        }

        var index = HitTest(e.Location);
        if (index != _hoverIndex)
        {
            _hoverIndex = index;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverIndex = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (Enabled && e.Button == MouseButtons.Left)
        {
            Focus();
            var index = HitTest(e.Location);
            if (index >= 0)
            {
                SelectedIndex = index;
            }
        }

        base.OnMouseDown(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Left)
        {
            SelectedIndex = Math.Max(0, _selectedIndex - 1);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Right)
        {
            SelectedIndex = Math.Min(Items.Length - 1, _selectedIndex + 1);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        _hoverIndex = -1;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    private int HitTest(Point point)
    {
        if (Items.Length == 0 || !ClientRectangle.Contains(point))
        {
            return -1;
        }

        var width = (Width - 6f) / Items.Length;
        return Math.Clamp((int)((point.X - 3) / width), 0, Items.Length - 1);
    }
}
