using System.Diagnostics;
using ApexTools.Controls;
using ApexTools.Core;

namespace ApexTools;

public class AudioExtractorPanel : Panel
{
    private static readonly Color TextSubtle = Color.FromArgb(120, 120, 135);
    private static readonly Color TextWhite = Color.FromArgb(240, 240, 245);
    private static readonly Color BgPanel = Color.FromArgb(22, 22, 30);

    private string _urlOrQuery = "";
    private string _outputDir = "";
    private string _audioFormat = "mp3";
    private readonly Label _statusLabel;
    private readonly ProgressBar _progressBar;
    private readonly Label _progressText;

    public AudioExtractorPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;
        Padding = new Padding(20);

        var main = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false
        };

        // URL/Query input
        var inputRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 8) };
        var urlInput = new TextBox
        {
            Size = new Size(400, 30),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Canción o enlace de YouTube/TikTok"
        };
        var btnExtract = new GlassButton { Text = "Extraer Audio", Width = 140, Height = 34 };
        btnExtract.Click += (s, e) => ExtractAudio(urlInput.Text);

        inputRow.Controls.AddRange(new Control[] { urlInput, btnExtract });
        main.Controls.Add(inputRow);

        // Format selector
        var fmtRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 6, 0, 0) };
        fmtRow.Controls.Add(MakeLabel("Formato:"));
        var fmtCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = TextWhite,
            FlatStyle = FlatStyle.Flat,
            Width = 120, Height = 28
        };
        fmtCombo.Items.AddRange(new object[] { "mp3", "wav", "m4a", "flac" });
        fmtCombo.SelectedIndex = 0;
        _audioFormat = "mp3";
        fmtCombo.SelectedIndexChanged += (s, e) => _audioFormat = fmtCombo.SelectedItem.ToString()!;
        fmtRow.Controls.AddRange(new Control[] { fmtCombo, MakeLabel("  Calidad: 320kbps") });
        main.Controls.Add(fmtRow);

        // Output dir
        var outRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        _outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music", "Apex_Audio");
        var outInput = new TextBox
        {
            Size = new Size(350, 28),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
            Text = _outputDir
        };
        var btnBrowse = new GlassButton { Text = "Examinar", Width = 80, Height = 28, BackColor = Color.FromArgb(60, 60, 70), ForeColor = TextWhite };
        btnBrowse.Click += (s, e) => { using var d = new FolderBrowserDialog(); if (d.ShowDialog() == DialogResult.OK) { _outputDir = d.SelectedPath; outInput.Text = _outputDir; } };
        outRow.Controls.AddRange(new Control[] { outInput, btnBrowse });
        main.Controls.Add(outRow);

        // Progress
        _progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 6, Style = ProgressBarStyle.Continuous };
        _progressText = new Label { Dock = DockStyle.Top, Height = 24, ForeColor = TextSubtle, Font = new Font("Segoe UI", 9f) };
        main.Controls.AddRange(new Control[] { _progressBar, _progressText });

        // Status
        _statusLabel = new Label { Dock = DockStyle.Top, Height = 30, ForeColor = TextSubtle, Font = new Font("Segoe UI", 10f), TextAlign = ContentAlignment.MiddleCenter, Visible = false };
        main.Controls.Add(_statusLabel);

        Controls.Add(main);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text, AutoSize = true, ForeColor = TextSubtle,
        Font = new Font("Segoe UI", 9f), Margin = new Padding(0, 4, 4, 0)
    };

    private void ExtractAudio(string urlOrQuery)
    {
        _urlOrQuery = urlOrQuery;
        _statusLabel.Visible = true;
        _statusLabel.Text = "Iniciando extracción...";
        _progressBar.Value = 0;
        _progressText.Text = "Preparando...";

        try
        {
            var dlDir = _outputDir;
            Directory.CreateDirectory(dlDir);

            // Run yt-dlp in a background manner
            var task = YtDlpHelper.DownloadAsync(_urlOrQuery, dlDir, log: s => _statusLabel.Text = s)!;
            task.Wait();

            if (task.IsCompleted && task.Result.Item1 != null)
            {
                var downloadedPath = task.Result.Item1;
                var meta = task.Result.Item2;
                var (artist, title) = YtDlpHelper.GuessArtistTitle(meta);
                var ext = _audioFormat;

                // Find the audio file
                var files = Directory.GetFiles(dlDir)
                    .Where(f => f.EndsWith($".{ext}", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (files.Count > 0)
                {
                    var audioPath = files[0];
                    _progressBar.Value = 100;
                    _progressText.Text = $"¡Audio extraído!\n{Path.GetFileName(audioPath)}";
                    _statusLabel.Visible = true;
                    _statusLabel.Text = $"✅ {title} - {artist}";

                    ShowMsg($"Guardado en: {Path.GetDirectoryName(audioPath)}");
                }
                else
                {
                    ShowMsg("⚠️ No se encontró el archivo de audio generado");
                    _statusLabel.Text = "⚠️ No se encontró el archivo";
                }
            }
            else
            {
                ShowMsg($"❌ Error: {task.Exception?.Message ?? "Error desconocido"}");
                _statusLabel.Text = $"❌ Error: {task.Exception?.Message ?? "Error desconocido"}";
                _statusLabel.Visible = true;
            }
        }
        catch (Exception ex)
        {
            ShowMsg($"❌ Error crítico: {ex.Message}");
            _statusLabel.Text = $"❌ Error crítico: {ex.Message}";
            _statusLabel.Visible = true;
        }
    }

    private static void ShowMsg(string msg) => Debug.WriteLine($"[Audio] {msg}");
}