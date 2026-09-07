using System.Diagnostics;
using ApexTools.Controls;
using ApexTools.Core;
using ApexTools.Models;

namespace ApexTools;

public class ShortCutPanel : Panel
{
    private string _videoPath = "";
    private readonly List<SongSegment> _songs = new();
    private readonly List<QueueItem> _queue = new();

    private TextBox _urlInput = null!;
    private GlassButton _btnDownload = null!;
    private GlassButton _btnSelectFile = null!;
    private GlassButton _btnDetect = null!;
    private GlassButton _btnCut = null!;
    private Label _videoInfo = null!;
    private Label _videoName = null!;
    private Label _songCount = null!;
    private Label _queueCount = null!;
    private Panel _songsContainer = null!;
    private Panel _queueContainer = null!;
    private Label _emptyState = null!;
    private Label _queueEmpty = null!;
    private NumericUpDown _noiseDb = null!;
    private NumericUpDown _minSilence = null!;
    private ComboBox _formatCombo = null!;
    private TextBox _renameInput = null!;
    private TextBox _outDirInput = null!;
    private Panel _progressPanel = null!;
    private Label _progressText = null!;
    private ProgressBar _progressBar = null!;

    private static readonly Color Accent = Color.FromArgb(251, 191, 36);
    private static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
    private static readonly Color TextWhite = Color.FromArgb(240, 240, 245);
    private static readonly Color TextSubtle = Color.FromArgb(120, 120, 135);

    public ShortCutPanel()
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        BackColor = Color.Transparent;
        BuildUI();
    }

    private void BuildUI()
    {
        var mainLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(8)
        };

        mainLayout.Controls.Add(BuildInputSection());
        mainLayout.Controls.Add(BuildSongsSection());
        mainLayout.Controls.Add(BuildQueueSection());
        mainLayout.Controls.Add(BuildOutputSection());

        Controls.Add(mainLayout);
    }

    private GlassPanel BuildInputSection()
    {
        var panel = new GlassPanel { Dock = DockStyle.Top, Padding = new Padding(14), AutoSize = true };

        _urlInput = new TextBox
        {
            Size = new Size(480, 32),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f),
            PlaceholderText = "Paste a video URL (YouTube, etc.)..."
        };
        _urlInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) DownloadUrl(); };

        _btnDownload = new GlassButton { Text = "Download", Width = 100, Height = 34 };
        _btnDownload.Click += (s, e) => DownloadUrl();

        _btnSelectFile = new GlassButton { Text = "Select File", Width = 100, Height = 34, BackColor = Color.FromArgb(60, 60, 70), ForeColor = TextWhite };
        _btnSelectFile.Click += (s, e) => SelectFile();

        var urlRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        urlRow.Controls.AddRange(new Control[] { _urlInput, _btnDownload, _btnSelectFile });
        panel.Controls.Add(urlRow);

        _videoInfo = new Label
        {
            Dock = DockStyle.Top, Height = 36, Visible = false,
            BackColor = Color.FromArgb(20, Accent),
            ForeColor = TextWhite, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
        };
        panel.Controls.Add(_videoInfo);

        _videoName = new Label { Dock = DockStyle.Top, Height = 20, Visible = false, ForeColor = TextSubtle, Font = new Font("Segoe UI", 9f) };
        panel.Controls.Add(_videoName);

        var settingsRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        _noiseDb = new NumericUpDown { Minimum = -60, Maximum = 0, Value = -32, Width = 60, BackColor = Color.FromArgb(30, 30, 40), ForeColor = TextWhite };
        _minSilence = new NumericUpDown { Minimum = 0.1m, Maximum = 5, Value = 0.7m, Width = 60, DecimalPlaces = 1, Increment = 0.1m, BackColor = Color.FromArgb(30, 30, 40), ForeColor = TextWhite };
        settingsRow.Controls.Add(MakeLabel("Silence dB:"));
        settingsRow.Controls.Add(_noiseDb);
        settingsRow.Controls.Add(MakeLabel("Min Silence (s):"));
        settingsRow.Controls.Add(_minSilence);
        panel.Controls.Add(settingsRow);

        var actionRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        _btnDetect = new GlassButton { Text = "Analyze Songs", Width = 160, Height = 38 };
        _btnDetect.Click += (s, e) => DetectSongs();
        var btnAddSong = new GlassButton { Text = "+ Add Segment", Width = 130, Height = 38, BackColor = Color.FromArgb(60, 60, 70), ForeColor = TextWhite };
        btnAddSong.Click += (s, e) => AddSong();
        actionRow.Controls.AddRange(new Control[] { _btnDetect, btnAddSong });
        panel.Controls.Add(actionRow);

        return panel;
    }

    private GlassPanel BuildSongsSection()
    {
        var panel = new GlassPanel { Dock = DockStyle.Top, Padding = new Padding(14), AutoSize = true };

        var header = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        _songCount = new Label
        {
            Text = "0 detected",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Accent,
            AutoSize = true,
            BackColor = Color.FromArgb(25, Accent),
            Padding = new Padding(8, 2, 8, 2),
            Margin = new Padding(8, 0, 0, 0)
        };
        header.Controls.Add(MakeLabel("SEGMENTS"));
        header.Controls.Add(_songCount);
        panel.Controls.Add(header);

        _emptyState = new Label
        {
            Text = "Load a video and click 'Analyze Songs' to detect segments automatically.",
            Dock = DockStyle.Top, Height = 50,
            ForeColor = TextSubtle, TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10f)
        };
        panel.Controls.Add(_emptyState);

        _songsContainer = new Panel { Dock = DockStyle.Top, AutoScroll = true, Height = 200, BackColor = Color.Transparent };
        panel.Controls.Add(_songsContainer);

        return panel;
    }

    private GlassPanel BuildQueueSection()
    {
        var panel = new GlassPanel { Dock = DockStyle.Top, Padding = new Padding(14), AutoSize = true };

        var header = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        _queueCount = new Label
        {
            Text = "0 videos",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = AccentBlue,
            AutoSize = true,
            BackColor = Color.FromArgb(25, AccentBlue),
            Padding = new Padding(8, 2, 8, 2),
            Margin = new Padding(8, 0, 0, 0)
        };
        header.Controls.Add(MakeLabel("BATCH QUEUE"));
        header.Controls.Add(_queueCount);
        panel.Controls.Add(header);

        _queueEmpty = new Label
        {
            Text = "Use 'Add to Queue' to process multiple videos",
            Dock = DockStyle.Top, Height = 30,
            ForeColor = TextSubtle, TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9f)
        };
        panel.Controls.Add(_queueEmpty);

        _queueContainer = new Panel { Dock = DockStyle.Top, AutoScroll = true, Height = 100, BackColor = Color.Transparent };
        panel.Controls.Add(_queueContainer);

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 6, 0, 0) };
        var btnAddQ = new GlassButton { Text = "+ Queue", Width = 80, Height = 30, BackColor = Color.FromArgb(60, 60, 70), ForeColor = TextWhite };
        btnAddQ.Click += (s, e) => AddToQueue();
        var btnAnalyzeQ = new GlassButton { Text = "Analyze", Width = 80, Height = 30, BackColor = Color.FromArgb(60, 60, 70), ForeColor = TextWhite };
        btnAnalyzeQ.Click += (s, e) => AnalyzeQueue();
        var btnProcessQ = new GlassButton { Text = "Process", Width = 80, Height = 30 };
        btnProcessQ.Click += (s, e) => ProcessQueue();
        var btnClearQ = new GlassButton { Text = "Clear", Width = 60, Height = 30, BackColor = Color.FromArgb(120, 40, 40), ForeColor = Color.FromArgb(248, 113, 113) };
        btnClearQ.Click += (s, e) => { _queue.Clear(); UpdateQueueUI(); };
        btnRow.Controls.AddRange(new Control[] { btnAddQ, btnAnalyzeQ, btnProcessQ, btnClearQ });
        panel.Controls.Add(btnRow);

        return panel;
    }

    private GlassPanel BuildOutputSection()
    {
        var panel = new GlassPanel { Dock = DockStyle.Top, Padding = new Padding(14), AutoSize = true };

        var fmtRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        _formatCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = TextWhite,
            FlatStyle = FlatStyle.Flat,
            Width = 220, Height = 30
        };
        _formatCombo.Items.AddRange(new object[]
        {
            "MP4 High (CRF 18)", "MP4 Medium (CRF 23)", "MP4 Fast (CRF 28)",
            "WebM VP9 High (CRF 24)", "WebM VP9 Medium (CRF 32)"
        });
        _formatCombo.SelectedIndex = 1;
        fmtRow.Controls.Add(MakeLabel("Format:"));
        fmtRow.Controls.Add(_formatCombo);
        fmtRow.Controls.Add(MakeLabel("  Rename:"));
        _renameInput = new TextBox
        {
            Size = new Size(180, 28),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "{artist} - {title}"
        };
        fmtRow.Controls.Add(_renameInput);
        panel.Controls.Add(fmtRow);

        var hintLabel = new Label { Text = "  Vars: {artist} {title} {idx}", Dock = DockStyle.Top, Height = 16, ForeColor = Color.FromArgb(80, 80, 90), Font = new Font("Segoe UI", 8f) };
        panel.Controls.Add(hintLabel);

        var outRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 6, 0, 0) };
        _outDirInput = new TextBox
        {
            Size = new Size(380, 28),
            BackColor = Color.FromArgb(30, 30, 40),
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "shorts_apex")
        };
        var btnBrowse = new GlassButton { Text = "Browse", Width = 70, Height = 28, BackColor = Color.FromArgb(60, 60, 70), ForeColor = TextWhite };
        btnBrowse.Click += (s, e) => { using var d = new FolderBrowserDialog(); if (d.ShowDialog() == DialogResult.OK) _outDirInput.Text = d.SelectedPath; };
        outRow.Controls.AddRange(new Control[] { _outDirInput, btnBrowse });
        panel.Controls.Add(outRow);

        _btnCut = new GlassButton { Text = "CUT ALL SHORTS", Dock = DockStyle.Top, Height = 46, Margin = new Padding(0, 12, 0, 0) };
        _btnCut.Click += (s, e) => CutAll();
        panel.Controls.Add(_btnCut);

        _progressPanel = new Panel { Dock = DockStyle.Top, Height = 46, Visible = false, BackColor = Color.Transparent };
        _progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 6, Style = ProgressBarStyle.Continuous };
        _progressText = new Label { Dock = DockStyle.Top, Height = 24, ForeColor = TextSubtle, Font = new Font("Segoe UI", 9f) };
        _progressPanel.Controls.Add(_progressText);
        _progressPanel.Controls.Add(_progressBar);
        panel.Controls.Add(_progressPanel);

        return panel;
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text, AutoSize = true, ForeColor = TextSubtle,
        Font = new Font("Segoe UI", 9f), TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding(0, 6, 4, 0)
    };

    // ─── Actions ───────────────────────────────────────────────────────────

    private async void DownloadUrl()
    {
        var url = _urlInput.Text.Trim();
        if (string.IsNullOrEmpty(url)) { ShowMsg("Paste a URL first"); return; }
        _urlInput.Text = "";
        _btnDownload.SetLoading(true, "Downloading...");
        try
        {
            var dlDir = Path.Combine(AppContext.BaseDirectory, "shortcut_downloads");
            var (path, meta) = await YtDlpHelper.DownloadAsync(url, dlDir, log: ShowMsg);
            var (artist, title) = YtDlpHelper.GuessArtistTitle(meta);
            SetVideoLoaded(path, artist, title);
        }
        catch (Exception ex) { ShowMsg($"Error: {ex.Message}"); }
        finally { _btnDownload.SetLoading(false); }
    }

    private void SelectFile()
    {
        using var dlg = new OpenFileDialog { Filter = "Video files|*.mp4;*.mkv;*.mov;*.webm;*.avi|All files|*.*" };
        if (dlg.ShowDialog() == DialogResult.OK)
            SetVideoLoaded(dlg.FileName, "Unknown", Path.GetFileNameWithoutExtension(dlg.FileName));
    }

    private void SetVideoLoaded(string path, string artist, string title)
    {
        _videoPath = path;
        _videoInfo.Visible = true;
        _videoInfo.Text = $"  Loaded: {Path.GetFileName(path)}";
        _videoName.Visible = true;
        _videoName.Text = $"  {artist} - {title}";
        ShowMsg($"Video loaded: {Path.GetFileName(path)}");
    }

    private async void DetectSongs()
    {
        if (string.IsNullOrEmpty(_videoPath)) { ShowMsg("Load a video first"); return; }
        _btnDetect.SetLoading(true, "Analyzing...");
        try
        {
            var songs = await ShortCutEngine.DetectSongsAsync(_videoPath, (int)_noiseDb.Value, (double)_minSilence.Value, ShowMsg);
            _songs.Clear();
            _songs.AddRange(songs);
            UpdateSongsUI();
            ShowMsg($"{songs.Count} song(s) detected");
        }
        catch (Exception ex) { ShowMsg($"Error: {ex.Message}"); }
        finally { _btnDetect.SetLoading(false); }
    }

    private void AddSong()
    {
        var lastEnd = _songs.Count > 0 ? _songs[^1].End : 0;
        _songs.Add(new SongSegment { Index = _songs.Count + 1, Start = lastEnd, End = Math.Min(lastEnd + 58, lastEnd + 120), Title = $"Song {_songs.Count + 1}" });
        UpdateSongsUI();
    }

    private async void CutAll()
    {
        if (string.IsNullOrEmpty(_videoPath)) { ShowMsg("Load a video first"); return; }
        if (_songs.Count == 0) { ShowMsg("No songs detected"); return; }
        var outDir = _outDirInput.Text.Trim();
        if (string.IsNullOrEmpty(outDir)) { ShowMsg("Select an output folder"); return; }

        var format = (OutputFormat)_formatCombo.SelectedIndex;
        var rename = _renameInput.Text.Trim();

        _btnCut.SetLoading(true, "Cutting...");
        _progressPanel.Visible = true;
        _progressBar.Value = 10;
        _progressText.Text = "Preparing...";

        try
        {
            Directory.CreateDirectory(outDir);
            int total = 0;
            foreach (var song in _songs)
            {
                _progressText.Text = $"Cutting song {song.Index}...";
                var paths = await ShortCutEngine.ProcessSongAsync(_videoPath, song, outDir, format, string.IsNullOrEmpty(rename) ? null : rename, ShowMsg);
                total += paths.Count;
                _progressBar.Value = Math.Min(90, 10 + (song.Index * 80 / _songs.Count));
            }
            _progressBar.Value = 100;
            _progressText.Text = $"Done! {total} short(s) generated";
            ShowMsg($"{total} short(s) in: {outDir}");
        }
        catch (Exception ex) { ShowMsg($"Error: {ex.Message}"); }
        finally
        {
            _btnCut.SetLoading(false);
            await Task.Delay(2000);
            _progressPanel.Visible = false;
            _progressBar.Value = 0;
        }
    }

    private void AddToQueue()
    {
        if (string.IsNullOrEmpty(_videoPath)) { ShowMsg("Load a video first"); return; }
        _queue.Add(new QueueItem { Id = _queue.Count + 1, Path = _videoPath, Title = Path.GetFileNameWithoutExtension(_videoPath), Status = "pending" });
        UpdateQueueUI();
    }

    private async void AnalyzeQueue()
    {
        var pending = _queue.Where(q => q.Status == "pending").ToList();
        if (pending.Count == 0) { ShowMsg("No pending videos"); return; }
        foreach (var item in pending)
        {
            try
            {
                item.Status = "analyzing";
                UpdateQueueUI();
                item.Songs = await ShortCutEngine.DetectSongsAsync(item.Path, (int)_noiseDb.Value, (double)_minSilence.Value, ShowMsg);
                item.Status = "ready";
            }
            catch (Exception ex) { item.Status = "error"; item.Error = ex.Message; }
            UpdateQueueUI();
        }
    }

    private async void ProcessQueue()
    {
        var ready = _queue.Where(q => q.Status == "ready" && q.Songs != null).ToList();
        if (ready.Count == 0) { ShowMsg("No ready videos"); return; }
        var outDir = _outDirInput.Text.Trim();
        var format = (OutputFormat)_formatCombo.SelectedIndex;
        var rename = _renameInput.Text.Trim();
        int total = 0;

        foreach (var item in ready)
        {
            try
            {
                item.Status = "processing";
                UpdateQueueUI();
                Directory.CreateDirectory(outDir);
                foreach (var song in item.Songs!)
                {
                    var paths = await ShortCutEngine.ProcessSongAsync(item.Path, song, outDir, format, string.IsNullOrEmpty(rename) ? null : rename, ShowMsg);
                    total += paths.Count;
                }
                item.Status = "done";
            }
            catch (Exception ex) { item.Status = "error"; item.Error = ex.Message; }
            UpdateQueueUI();
        }
        ShowMsg($"Queue complete. {total} short(s).");
    }

    // ─── UI Updates ────────────────────────────────────────────────────────

    private void UpdateSongsUI()
    {
        _songsContainer.Controls.Clear();
        _songCount.Text = $"{_songs.Count} detected";
        _emptyState.Visible = _songs.Count == 0;

        int y = 0;
        foreach (var song in _songs)
        {
            var card = new Panel { Location = new Point(0, y), Size = new Size(_songsContainer.Width - 20, 44), BackColor = Color.FromArgb(20, 255, 255, 255) };
            var idx = new Label { Text = song.Index.ToString(), Location = new Point(6, 8), Size = new Size(26, 26), BackColor = Color.FromArgb(25, Accent), ForeColor = Accent, Font = new Font("Segoe UI", 9f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            var info = new Label { Text = $"{song.Artist} - {song.Title}  |  {song.Start:F1}s - {song.End:F1}s ({song.Duration:F1}s)", Location = new Point(40, 6), Size = new Size(380, 32), ForeColor = TextWhite, Font = new Font("Segoe UI", 9f) };
            var dur = new Label { Text = song.Duration > ShortCutEngine.ShortMaxSeconds ? "Fade" : "Ready", Location = new Point(430, 12), Size = new Size(50, 20), ForeColor = song.Duration > ShortCutEngine.ShortMaxSeconds ? Color.FromArgb(248, 113, 113) : Color.FromArgb(52, 211, 153), Font = new Font("Segoe UI", 8f, FontStyle.Bold) };
            var btnE = new Button { Text = "Edit", Location = new Point(490, 8), Size = new Size(44, 26), FlatStyle = FlatStyle.Flat, ForeColor = TextWhite, BackColor = Color.FromArgb(40, 40, 50), Font = new Font("Segoe UI", 8f), Tag = song };
            btnE.Click += (s, e) => EditSong(song);
            var btnX = new Button { Text = "X", Location = new Point(540, 8), Size = new Size(24, 26), FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(248, 113, 113), BackColor = Color.FromArgb(40, 40, 50), Font = new Font("Segoe UI", 8f), Tag = song };
            btnX.Click += (s, e) => { _songs.Remove(song); for (int i = 0; i < _songs.Count; i++) _songs[i].Index = i + 1; UpdateSongsUI(); };
            card.Controls.AddRange(new Control[] { idx, info, dur, btnE, btnX });
            _songsContainer.Controls.Add(card);
            y += 48;
        }
    }

    private void EditSong(SongSegment song)
    {
        using var dlg = new Form { Text = "Edit Segment", Size = new Size(360, 260), StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(22, 22, 30), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        int y = 16;
        var aI = AddEditRow(dlg, "Artist:", song.Artist, ref y);
        var tI = AddEditRow(dlg, "Song:", song.Title, ref y);
        var sI = AddEditRow(dlg, "Start (s):", song.Start.ToString("F1"), ref y);
        var eI = AddEditRow(dlg, "End (s):", song.End.ToString("F1"), ref y);
        var btnSave = new GlassButton { Text = "Save", Location = new Point(160, y + 8), Width = 80, Height = 32 };
        var btnCancel = new GlassButton { Text = "Cancel", Location = new Point(250, y + 8), Width = 80, Height = 32, BackColor = Color.FromArgb(60, 60, 70), ForeColor = TextWhite };
        btnSave.Click += (s, e) =>
        {
            song.Artist = aI.Text; song.Title = tI.Text;
            if (double.TryParse(sI.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var st)) song.Start = st;
            if (double.TryParse(eI.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var en)) song.End = en;
            UpdateSongsUI(); dlg.Close();
        };
        btnCancel.Click += (s, e) => dlg.Close();
        dlg.Controls.AddRange(new Control[] { aI, tI, sI, eI, btnSave, btnCancel });
        dlg.ShowDialog();
    }

    private TextBox AddEditRow(Form form, string label, string value, ref int y)
    {
        form.Controls.Add(new Label { Text = label, Location = new Point(16, y + 3), AutoSize = true, ForeColor = TextSubtle });
        var txt = new TextBox { Location = new Point(90, y), Size = new Size(220, 26), BackColor = Color.FromArgb(30, 30, 40), ForeColor = TextWhite, BorderStyle = BorderStyle.FixedSingle, Text = value };
        y += 34;
        return txt;
    }

    private void UpdateQueueUI()
    {
        _queueContainer.Controls.Clear();
        _queueCount.Text = $"{_queue.Count} video(s)";
        _queueEmpty.Visible = _queue.Count == 0;

        int y = 0;
        foreach (var item in _queue)
        {
            var color = item.Status switch
            {
                "pending" => TextSubtle,
                "analyzing" => AccentBlue,
                "ready" => Color.FromArgb(52, 211, 153),
                "processing" => Accent,
                "done" => Color.FromArgb(52, 211, 153),
                "error" => Color.FromArgb(248, 113, 113),
                _ => TextSubtle
            };
            var info = new Label
            {
                Text = $"[{item.Status}] {item.Title}{(item.Songs != null ? $" ({item.Songs.Count} songs)" : "")}{(item.Error != null ? $" - {item.Error[..Math.Min(30, item.Error.Length)]}" : "")}",
                Location = new Point(4, y + 4), Size = new Size(450, 24),
                ForeColor = color, Font = new Font("Segoe UI", 9f)
            };
            _queueContainer.Controls.Add(info);
            y += 32;
        }
    }

    private static void ShowMsg(string msg) => Debug.WriteLine($"[ShortCut] {msg}");
}
