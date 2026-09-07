using System.Diagnostics;

namespace ApexTools.Core;

public static class YtDlpHelper
{
    private static string? _ytdlpPath;

    public static string YtDlp => _ytdlpPath ??= FindYtDlp();

    private static string FindYtDlp()
    {
        var exeName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "yt-dlp.exe" : "yt-dlp";

        var appDir = AppContext.BaseDirectory;
        var local = Path.Combine(appDir, exeName);
        if (File.Exists(local)) return local;

        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            var full = Path.Combine(dir.Trim(), exeName);
            if (File.Exists(full)) return full;
        }

        return exeName;
    }

    public static async Task<(string path, Dictionary<string, string> meta)> DownloadAsync(
        string url, string outDir, int maxRetries = 3,
        Action<string>? log = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outDir);
        var template = Path.Combine(outDir, "%(id)s.%(ext)s");
        string? lastError = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (attempt > 1)
                log?.Invoke($"Retry {attempt}/{maxRetries}...");

            var psi = new ProcessStartInfo
            {
                FileName = YtDlp,
                Arguments = $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\" " +
                            $"--merge-output-format mp4 --no-playlist -o \"{template}\" \"{url}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(psi)!;
            _ = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode == 0) break;
            lastError = $"yt-dlp exit code {proc.ExitCode}";
            log?.Invoke($"Attempt {attempt} failed: {lastError}");
        }

        // Find downloaded file
        var files = Directory.GetFiles(outDir)
            .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .FirstOrDefault();

        if (files == null)
            throw new Exception("No downloaded file found");

        var meta = await GetMetaAsync(url);
        return (files, meta);
    }

    public static async Task<Dictionary<string, string>> GetMetaAsync(string url)
    {
        var meta = new Dictionary<string, string>();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = YtDlp,
                Arguments = $"-J --no-playlist --skip-download \"{url}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi)!;
            var json = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode == 0)
            {
                // Simple JSON parsing without Newtonsoft
                meta["title"] = ExtractJsonString(json, "title");
                meta["uploader"] = ExtractJsonString(json, "uploader");
                meta["artist"] = ExtractJsonString(json, "artist");
                meta["track"] = ExtractJsonString(json, "track");
            }
        }
        catch { }
        return meta;
    }

    private static string ExtractJsonString(string json, string key)
    {
        var idx = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
        if (idx < 0) return "";
        var colonIdx = json.IndexOf(':', idx);
        if (colonIdx < 0) return "";
        var start = json.IndexOf('"', colonIdx + 1);
        if (start < 0) return "";
        var end = json.IndexOf('"', start + 1);
        if (end < 0) return "";
        return json.Substring(start + 1, end - start - 1);
    }

    public static (string artist, string title) GuessArtistTitle(Dictionary<string, string> meta)
    {
        var title = meta.GetValueOrDefault("title", "").Trim();
        var artist = meta.GetValueOrDefault("artist", "").Trim();
        var track = meta.GetValueOrDefault("track", "").Trim();

        if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(track))
            return (artist, track);

        // Try "Artist - Title" pattern
        var dashIdx = title.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIdx > 0 && dashIdx < 80)
            return (title[..dashIdx].Trim(), title[(dashIdx + 3)..].Trim());

        var uploader = meta.GetValueOrDefault("uploader", "").Trim();
        return (string.IsNullOrEmpty(uploader) ? "Unknown" : uploader,
                string.IsNullOrEmpty(title) ? "Sin titulo" : title);
    }
}
