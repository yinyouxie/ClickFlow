using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal class NumberStepper : Control
{
    private int _value;
    private int _min;
    private int _max = 100;
    private int _step = 1;
    private string _suffix = string.Empty;
    private bool _hoverLeft;
    private bool _hoverRight;
    private readonly TextBox _editor;
    private bool _syncing;

    public int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, Min, Max);
            if (_value == next)
            {
                return;
            }

            _value = next;
            SyncEditor();
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int Min
    {
        get => _min;
        set
        {
            _min = value;
            if (_value < _min)
            {
                Value = _min;
            }
        }
    }

    public int Max
    {
        get => _max;
        set
        {
            _max = Math.Max(value, _min);
            if (_value > _max)
            {
                Value = _max;
            }
        }
    }

    public int Step
    {
        get => _step;
        set => _step = Math.Max(1, value);
    }

    public string Suffix
    {
        get => _suffix;
        set
        {
            _suffix = value ?? string.Empty;
            SyncEditor();
            Invalidate();
        }
    }

    public string ZeroText { get; set; } = "0";

    public event EventHandler? ValueChanged;

    public NumberStepper()
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
        Height = 34;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.SpinButton;

        _editor = new TextBox
        {
            BorderStyle = BorderStyle.None,
            TextAlign = HorizontalAlignment.Center,
            BackColor = Theme.Field,
            ForeColor = Theme.Text,
            Font = Theme.BodyBold,
            ShortcutsEnabled = true,
            MaxLength = 10
        };
        _editor.KeyDown += EditorKeyDown;
        _editor.GotFocus += (_, _) =>
        {
            if (Value == 0 && _editor.Text == ZeroText)
            {
                _editor.Text = "0";
            }

            _editor.SelectAll();
        };
        _editor.LostFocus += (_, _) =>
        {
            CommitEditor();
            SyncEditor();
        };
        Controls.Add(_editor);
        LayoutEditor();
        SyncEditor();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var enabled = Enabled;
        var rect = new RectangleF(0, 0, Width, Height);
        Theme.FillRounded(
            e.Graphics,
            rect,
            9,
            enabled ? Theme.Field : Theme.Blend(Theme.Field, Theme.Bg, 0.55));
        Theme.DrawRounded(
            e.Graphics,
            rect,
            9,
            ContainsFocus && enabled ? Theme.Alpha(Theme.Blue, 120) : Theme.Border);

        var buttonWidth = 27f;
        var leftRect = new RectangleF(0, 0, buttonWidth, Height);
        var rightRect = new RectangleF(Width - buttonWidth, 0, buttonWidth, Height);

        if (enabled && _hoverLeft && Value > Min)
        {
            Theme.FillRounded(e.Graphics, RectangleF.Inflate(leftRect, -3, -3), 7, Theme.SurfaceHover);
        }

        if (enabled && _hoverRight && Value < Max)
        {
            Theme.FillRounded(e.Graphics, RectangleF.Inflate(rightRect, -3, -3), 7, Theme.SurfaceHover);
        }

        var leftColor = !enabled ? Theme.MutedDim : Value > Min ? Theme.Muted : Theme.MutedDim;
        var rightColor = !enabled ? Theme.MutedDim : Value < Max ? Theme.Muted : Theme.MutedDim;
        Theme.DrawGlyph(
            e.Graphics,
            Glyph.Minus,
            new RectangleF(leftRect.Left + 8, (Height - 12) / 2f, 12, 12),
            leftColor,
            1.5f);
        Theme.DrawGlyph(
            e.Graphics,
            Glyph.Plus,
            new RectangleF(rightRect.Left + 7, (Height - 12) / 2f, 12, 12),
            rightColor,
            1.5f);

    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutEditor();
    }

    private void LayoutEditor()
    {
        if (_editor is null)
        {
            return;
        }

        var height = Math.Max(12, Height - 10);
        _editor.SetBounds(30, 5, Math.Max(18, Width - 60), height);
        _editor.BackColor = Enabled ? Theme.Field : Theme.Blend(Theme.Field, Theme.Bg, 0.55);
        _editor.ForeColor = Enabled ? Theme.Text : Theme.MutedDim;
    }

    private void EditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            CommitEditor();
            SyncEditor();
            e.SuppressKeyPress = true;
            _editor.SelectAll();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            SyncEditor();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode is Keys.Up or Keys.Down)
        {
            Adjust(e.KeyCode == Keys.Up ? Step : -Step);
            SyncEditor();
            e.SuppressKeyPress = true;
        }
    }

    private void CommitEditor()
    {
        var text = _editor.Text.Trim();
        if (!string.IsNullOrEmpty(_suffix) &&
            text.EndsWith(_suffix, StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^_suffix.Length].Trim();
        }

        if (text == ZeroText && Value == 0)
        {
            return;
        }

        text = text.Replace(",", string.Empty).Replace(" ", string.Empty);
        if (int.TryParse(text, out var parsed))
        {
            Value = parsed;
        }
    }

    private void SyncEditor()
    {
        if (_editor == null || _syncing)
        {
            return;
        }

        _syncing = true;
        var text = Value == 0 && !string.IsNullOrEmpty(ZeroText)
            ? ZeroText
            : Value.ToString();
        if (!string.IsNullOrEmpty(_suffix))
        {
            text += " " + _suffix;
        }

        _editor.Text = text;
        _syncing = false;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!Enabled)
        {
            if (_hoverLeft || _hoverRight)
            {
                _hoverLeft = false;
                _hoverRight = false;
                Invalidate();
            }

            return;
        }

        var left = e.X < 30;
        var right = e.X > Width - 30;
        if (left != _hoverLeft || right != _hoverRight)
        {
            _hoverLeft = left;
            _hoverRight = right;
            Cursor = Cursors.Hand;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverLeft = false;
        _hoverRight = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!Enabled)
        {
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            Focus();
            if (e.X < 32)
            {
                Adjust(-Step);
            }
            else if (e.X > Width - 32)
            {
                Adjust(Step);
            }
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (Enabled && ContainsFocus)
        {
            Adjust(e.Delta > 0 ? Step : -Step);
            SyncEditor();
        }

        base.OnMouseWheel(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        _editor.Enabled = Enabled;
        _hoverLeft = false;
        _hoverRight = false;
        LayoutEditor();
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down ||
               base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Up or Keys.Right)
        {
            Adjust(Step);
            e.Handled = true;
        }
        else if (e.KeyCode is Keys.Down or Keys.Left)
        {
            Adjust(-Step);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    private void Adjust(int amount)
    {
        Value += amount;
    }
}
