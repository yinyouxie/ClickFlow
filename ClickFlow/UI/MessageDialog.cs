using System.Drawing.Drawing2D;
using ClickFlow.UI.Controls;

namespace ClickFlow.UI;

internal sealed class MessageDialog : Form
{
    private readonly string _message;
    private readonly bool _danger;
    private readonly Label _titleLabel;
    private readonly Label _messageLabel;
    private readonly FlatButton _primaryButton;
    private readonly FlatButton? _secondaryButton;
    private bool _layingOut;

    private MessageDialog(
        string title,
        string message,
        string primary,
        string? secondary,
        bool danger)
    {
        _message = message;
        _danger = danger;

        SuspendLayout();
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Width = 500;
        Height = 270;
        Padding = new Padding(1);

        _titleLabel = new Label
        {
            Text = title,
            Font = Theme.Heading,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            AutoSize = false
        };
        _titleLabel.MouseDown += (_, _) => WindowEffects.BeginDrag(this);

        _messageLabel = new Label
        {
            Text = message,
            Font = Theme.Body,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            AutoSize = false
        };

        _primaryButton = new FlatButton
        {
            Text = primary,
            Style = FlatButtonStyle.Tint,
            AccentColor = danger ? Theme.Accent : Theme.Green,
            Size = new Size(100, 38),
            Radius = 10
        };
        _primaryButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(_titleLabel);
        Controls.Add(_messageLabel);
        Controls.Add(_primaryButton);

        if (!string.IsNullOrWhiteSpace(secondary))
        {
            _secondaryButton = new FlatButton
            {
                Text = secondary,
                Style = FlatButtonStyle.Ghost,
                Size = new Size(88, 38),
                Radius = 10
            };
            _secondaryButton.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(_secondaryButton);
        }

        AttachDrag(_titleLabel);
        AttachDrag(this);
        Resize += (_, _) => UpdateRegion();
        Layout += (_, _) => LayoutChildren();
        ResumeLayout(true);
    }

    public static bool Show(
        IWin32Window owner,
        string title,
        string message,
        string primary = "确定",
        string? secondary = null,
        bool danger = false)
    {
        using var dialog = new MessageDialog(title, message, primary, secondary, danger);
        return dialog.ShowDialog(owner) == DialogResult.OK;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowEffects.ApplyDarkChrome(this);
        UpdateRegion();
        LayoutChildren();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scale = DeviceDpi / 96f;
        Theme.DrawRounded(
            e.Graphics,
            new RectangleF(0.5f, 0.5f, Width - 1, Height - 1),
            18 * scale,
            Theme.Border,
            1);

        var accent = _danger ? Theme.Accent : Theme.Green;
        var icon = new RectangleF(26 * scale, 28 * scale, 38 * scale, 38 * scale);
        Theme.FillRounded(e.Graphics, icon, 11 * scale, Theme.Alpha(accent, 22));
        Theme.DrawRounded(e.Graphics, icon, 11 * scale, Theme.Alpha(accent, 55));
        Theme.DrawGlyph(
            e.Graphics,
            _danger ? Glyph.Clear : Glyph.Help,
            new RectangleF(icon.Left + 10 * scale, icon.Top + 10 * scale, 18 * scale, 18 * scale),
            accent,
            1.7f);
    }

    private void LayoutChildren()
    {
        if (_layingOut)
        {
            return;
        }

        _layingOut = true;
        try
        {
            var scale = WindowEffects.GetDpiScale(this);
            var targetWidth = (int)Math.Round(500 * scale);
            if (Width != targetWidth)
            {
                Width = targetWidth;
            }

            _primaryButton.Size = new Size((int)(100 * scale), (int)(38 * scale));
            if (_secondaryButton != null)
            {
                _secondaryButton.Size = new Size((int)(88 * scale), (int)(38 * scale));
            }

            var textWidth = Width - (int)(94 * scale);
            var measured = TextRenderer.MeasureText(
                _message,
                Theme.Body,
                new Size(textWidth, 1000),
                TextFormatFlags.WordBreak);
            var messageHeight = Math.Max((int)(48 * scale), measured.Height + (int)(6 * scale));
            var buttonTop = Math.Max((int)(178 * scale), (int)(88 * scale) + messageHeight + (int)(24 * scale));
            Height = buttonTop + (int)(58 * scale);

            _titleLabel.SetBounds((int)(82 * scale), (int)(31 * scale), textWidth, (int)(30 * scale));
            _messageLabel.SetBounds((int)(82 * scale), (int)(66 * scale), textWidth, messageHeight);

            var right = Width - (int)(26 * scale);
            _primaryButton.Location = new Point(right - _primaryButton.Width, buttonTop);
            if (_secondaryButton != null)
            {
                _secondaryButton.Location = new Point(
                    _primaryButton.Left - _secondaryButton.Width - (int)(10 * scale),
                    buttonTop);
            }

            UpdateRegion();
        }
        finally
        {
            _layingOut = false;
        }
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = Theme.Rounded(
            new RectangleF(0, 0, Width, Height),
            18 * WindowEffects.GetDpiScale(this));
        Region = new Region(path);
    }

    private void AttachDrag(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowEffects.BeginDrag(this);
            }
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return true;
        }

        if (keyData == Keys.Enter)
        {
            DialogResult = DialogResult.OK;
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.None)
        {
            DialogResult = DialogResult.Cancel;
        }

        base.OnFormClosing(e);
    }
}
