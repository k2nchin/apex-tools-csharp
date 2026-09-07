using System.Drawing.Drawing2D;

namespace ApexTools.Controls;

public class GlassPanel : Panel
{
    public Color AccentColor { get; set; } = Color.FromArgb(251, 191, 36);
    public int CornerRadius { get; set; } = 12;
    public float GlowOpacity { get; set; } = 0.08f;

    public GlassPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Margin = new Padding(0, 0, 0, 10);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = GetRoundedRect(rect, CornerRadius);

        // Background
        using (var brush = new SolidBrush(Color.FromArgb(18, 255, 255, 255)))
            g.FillPath(brush, path);

        // Accent glow
        using (var brush = new SolidBrush(Color.FromArgb((int)(GlowOpacity * 255), AccentColor)))
            g.FillPath(brush, path);

        // Border
        using (var pen = new Pen(Color.FromArgb(30, 255, 255, 255), 1))
            g.DrawPath(pen, path);

        base.OnPaint(e);
    }

    private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public class GlassButton : Button
{
    public Color AccentColor { get; set; } = Color.FromArgb(251, 191, 36);
    public Color AccentGlow { get; set; } = Color.FromArgb(90, 251, 191, 36);
    public int CornerRadius { get; set; } = 10;

    private bool _isHovered;
    private bool _isLoading;
    private string _loadingText = "";

    public GlassButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(251, 191, 36);
        ForeColor = Color.FromArgb(20, 20, 20);
        Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        Height = 42;
        FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 158, 11);
        FlatAppearance.MouseDownBackColor = Color.FromArgb(217, 119, 6);
    }

    public void SetLoading(bool loading, string text = "")
    {
        _isLoading = loading;
        _loadingText = text;
        Enabled = !loading;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = GetRoundedRect(rect, CornerRadius);

        // Shadow
        using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
        {
            var shadowRect = new Rectangle(2, 3, Width - 1, Height - 1);
            using var shadowPath = GetRoundedRect(shadowRect, CornerRadius);
            g.FillPath(shadowBrush, shadowPath);
        }

        // Background
        var bgColor = Enabled ? AccentColor : Color.FromArgb(80, 80, 80);
        if (_isHovered && Enabled) bgColor = Color.FromArgb(245, 158, 11);
        using (var brush = new SolidBrush(bgColor))
            g.FillPath(brush, path);

        // Glow
        using (var brush = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
            g.FillPath(brush, path);

        // Text
        var text = _isLoading ? _loadingText : Text;
        var font = _isLoading ? new Font(Font.FontFamily, Font.Size - 1f, FontStyle.Bold) : Font;
        var textSize = g.MeasureString(text, font);
        var textX = (Width - textSize.Width) / 2;
        var textY = (Height - textSize.Height) / 2;

        using (var brush = new SolidBrush(ForeColor))
            g.DrawString(text, font, brush, textX, textY);

        font.Dispose();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public class SidebarButton : Button
{
    public Color ActiveColor { get; set; } = Color.FromArgb(251, 191, 36);
    public bool IsActive { get; set; }
    public string Tag2 { get; set; } = "";

    private bool _isHovered;

    public SidebarButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        ForeColor = Color.FromArgb(140, 140, 140);
        Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        Height = 44;
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new Padding(12, 0, 0, 0);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        if (IsActive)
        {
            // Active background
            using var path = GetRoundedRect(rect, 10);
            using (var brush = new SolidBrush(Color.FromArgb(25, ActiveColor)))
                g.FillPath(brush, path);

            // Left accent bar
            using var barBrush = new SolidBrush(ActiveColor);
            g.FillRectangle(barBrush, 0, 8, 3, Height - 16);

            // Active text color
            using (var brush = new SolidBrush(ActiveColor))
            {
                var textRect = new Rectangle(Padding.Left, 0, Width - Padding.Left - 10, Height);
                TextRenderer.DrawText(g, Text, Font, textRect, ActiveColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }
        else if (_isHovered)
        {
            using var path = GetRoundedRect(rect, 10);
            using (var brush = new SolidBrush(Color.FromArgb(12, 255, 255, 255)))
                g.FillPath(brush, path);
            base.OnPaint(e);
        }
        else
        {
            base.OnPaint(e);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
