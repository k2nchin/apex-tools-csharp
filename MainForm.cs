using ApexTools.Controls;

namespace ApexTools;

public partial class MainForm : Form
{
    private readonly Panel _sidebar;
    private readonly Panel _contentArea;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly List<SidebarButton> _navButtons = new();
    private Panel? _activePanel;
    private readonly Dictionary<string, Panel> _panels = new();

    private static readonly Color BgDark = Color.FromArgb(14, 14, 18);
    private static readonly Color BgPanel = Color.FromArgb(22, 22, 30);
    private static readonly Color AccentAmber = Color.FromArgb(251, 191, 36);
    private static readonly Color AccentPurple = Color.FromArgb(139, 92, 246);
    private static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
    private static readonly Color AccentGreen = Color.FromArgb(52, 211, 153);
    private static readonly Color TextWhite = Color.FromArgb(240, 240, 245);
    private static readonly Color TextSubtle = Color.FromArgb(120, 120, 135);

    public MainForm()
    {
        SetupForm();
        _sidebar = CreateSidebar();
        _contentArea = CreateContentArea();
        _titleLabel = CreateTitleLabel();
        _subtitleLabel = CreateSubtitleLabel();

        Controls.Add(_contentArea);
        Controls.Add(_sidebar);
        Controls.Add(_titleLabel);
        Controls.Add(_subtitleLabel);

        CreateToolPanels();
        SwitchTool("shortcut");
    }

    private void SetupForm()
    {
        Text = "Apex Tools";
        Size = new Size(1200, 750);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgDark;
        ForeColor = TextWhite;
        Font = new Font("Segoe UI", 10f);
        FormBorderStyle = FormBorderStyle.Sizable;
    }

    private Panel CreateSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = Color.FromArgb(16, 16, 22),
            Padding = new Padding(0, 16, 0, 16)
        };

        var logo = new Label
        {
            Text = "APEX TOOLS",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = AccentAmber,
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(0, 10, 0, 0)
        };
        sidebar.Controls.Add(logo);

        var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(40, 255, 255, 255) };
        sidebar.Controls.Add(sep);

        var tools = new[]
        {
            ("shortcut", "ShortCut", "Shorts 9:16", "SC", AccentAmber),
            ("tiktok", "TikTok Pro", "Downloader", "TT", AccentPurple),
            ("canvas", "Canvas", "Spotify", "CV", AccentGreen),
            ("audio", "Audio", "Extractor", "AU", AccentBlue),
            ("resizer", "Resizer", "Reframe", "RZ", AccentAmber),
            ("subtitles", "Subtitles", "Auto-generate", "SB", AccentPurple),
        };

        int y = 10;
        foreach (var (id, name, tag, icon, color) in tools)
        {
            var btn = new SidebarButton
            {
                Text = $"  [{icon}]  {name}",
                Tag2 = id,
                Location = new Point(0, y),
                Width = sidebar.Width,
                ActiveColor = color,
                Dock = DockStyle.Top,
                Height = 44
            };
            btn.Click += (s, e) => SwitchTool(id);
            _navButtons.Add(btn);
            sidebar.Controls.Add(btn);
            y += 46;
        }

        sidebar.Controls.SetChildIndex(logo, sidebar.Controls.Count - 1);
        sidebar.Controls.SetChildIndex(sep, sidebar.Controls.Count - 1);

        return sidebar;
    }

    private Panel CreateContentArea()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgDark,
            Padding = new Padding(24, 16, 24, 16)
        };
    }

    private Label CreateTitleLabel()
    {
        return new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = TextWhite,
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(230, 10, 0, 0),
            BackColor = Color.Transparent
        };
    }

    private Label CreateSubtitleLabel()
    {
        return new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextSubtle,
            Dock = DockStyle.Top,
            Height = 24,
            Padding = new Padding(230, 0, 0, 8),
            BackColor = Color.Transparent
        };
    }

    private void CreateToolPanels()
    {
        // Placeholder panels
        foreach (var id in new string[] { "shortcut", "tiktok", "canvas", "audio", "resizer", "subtitles" })
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.Transparent
            };
            var title = id switch
            {
                "shortcut" => "ShortCut - Shorts 9:16",
                "tiktok" => "TikTok Pro",
                "canvas" => "Canvas - Spotify",
                "audio" => "Audio Extractor",
                "resizer" => "Resizer",
                "subtitles" => "Subtitles",
                _ => id
            };
            var label = new Label
            {
                Text = $"{title}\n\nImplementación pendiente...\n\nComing soon...",
                Font = new Font("Segoe UI", 14f),
                ForeColor = TextSubtle,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(label);
            _panels[id] = panel;
            _contentArea.Controls.Add(panel);
        }
    }

    private void SwitchTool(string toolId)
    {
        foreach (var btn in _navButtons)
            btn.IsActive = btn.Tag2 == toolId;

        foreach (var kvp in _panels)
            kvp.Value.Visible = kvp.Key == toolId;

        _activePanel = _panels.GetValueOrDefault(toolId);

        var titles = new Dictionary<string, (string title, string subtitle)>
        {
            ["shortcut"] = ("ShortCut", "Generate Shorts 9:16 from any video"),
            ["tiktok"] = ("TikTok Pro", "Download TikTok videos without watermark"),
            ["canvas"] = ("Spotify Canvas", "Generate looping Canvas videos"),
            ["audio"] = ("Audio Extractor", "Extract audio tracks from videos"),
            ["resizer"] = ("Resizer", "Reframe videos to different aspect ratios"),
            ["subtitles"] = ("Subtitles", "Auto-generate subtitles"),
        };

        if (titles.TryGetValue(toolId, out var t))
        {
            _titleLabel.Text = t.title;
            _subtitleLabel.Text = t.subtitle;
        }
    }
}