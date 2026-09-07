using System.Text.RegularExpressions;
using ApexTools.Models;

namespace ApexTools.Core;

public static class ShortCutEngine
{
    public const int ShortMaxSeconds = 58;
    public const int OutWidth = 1080;
    public const int OutHeight = 1920;

    public static string SafeName(string? s)
    {
        s = (s ?? "").Trim();
        s = Regex.Replace(s, @"[\\/:*?""<>|]+", "");
        s = s.Trim('.', ' ');
        return string.IsNullOrEmpty(s) ? "sin_nombre" : s;
    }

    public static async Task<List<SongSegment>> DetectSongsAsync(
        string videoPath, int noiseDb = -32, double minSilence = 0.7,
        Action<string>? log = null, CancellationToken ct = default)
    {
        log?.Invoke("Analyzing audio and detecting songs...");
        var duration = await FFmpegHelper.GetDurationAsync(videoPath);
        var silences = await FFmpegHelper.DetectSilencesAsync(videoPath, noiseDb, minSilence);

        var bounds = new List<double>();
        foreach (var (s, e) in silences)
        {
            var mid = (s + e) / 2.0;
            if (mid < 2.0 || mid > duration - 2.0) continue;
            bounds.Add(mid);
        }

        bounds.Sort();
        var merged = new List<double>();
        foreach (var b in bounds)
        {
            if (merged.Count > 0 && b - merged[^1] < 0.5) continue;
            merged.Add(b);
        }

        var points = new List<double> { 0.0 };
        points.AddRange(merged);
        points.Add(duration);

        var segments = new List<SongSegment>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            var s = points[i];
            var e = points[i + 1];
            if (e - s >= 10.0)
                segments.Add(new SongSegment
                {
                    Index = segments.Count + 1,
                    Start = Math.Round(s, 2),
                    End = Math.Round(e, 2),
                    Title = $"Song {segments.Count + 1}",
                    Artist = ""
                });
        }

        if (segments.Count == 0)
            segments.Add(new SongSegment
            {
                Index = 1, Start = 0, End = Math.Round(duration, 2),
                Title = "Song 1", Artist = ""
            });

        log?.Invoke($"Duration: {duration:F1}s | Silences: {silences.Count} | Songs: {segments.Count}");
        return segments;
    }

    public static async Task<List<string>> ProcessSongAsync(
        string videoPath, SongSegment song, string outDir,
        OutputFormat format = OutputFormat.Mp4Med,
        string? renamePattern = null,
        Action<string>? log = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outDir);

        var artist = SafeName(string.IsNullOrEmpty(song.Artist) ? "artista" : song.Artist);
        var title = SafeName(string.IsNullOrEmpty(song.Title) ? $"song_{song.Index}" : song.Title);

        string baseName;
        if (!string.IsNullOrWhiteSpace(renamePattern))
        {
            baseName = renamePattern
                .Replace("{artist}", artist)
                .Replace("{title}", title)
                .Replace("{idx}", song.Index.ToString())
                .Replace("{n}", song.Index.ToString());
            baseName = SafeName(baseName);
        }
        else
        {
            baseName = $"{artist} - {title}";
        }

        var fmt = FormatPresets.Get(format);
        var paths = new List<string>();
        var chunks = Chunks(song.Start, song.End);

        for (int i = 0; i < chunks.Count; i++)
        {
            var (cs, ce) = chunks[i];
            var dur = ce - cs;
            var suffix = chunks.Count > 1 ? $" (part {i + 1})" : "";
            var outName = $"{baseName}{suffix}.{fmt.ext}";
            var outPath = Path.Combine(outDir, outName);

            log?.Invoke($"Cutting: {outName} [{cs:F1}s -> {ce:F1}s]");
            await FFmpegHelper.CutShortAsync(videoPath, cs, dur, outPath, format, ct);
            paths.Add(outPath);
        }

        return paths;
    }

    private static List<(double start, double end)> Chunks(double start, double end, int maxLen = ShortMaxSeconds)
    {
        var chunks = new List<(double, double)>();
        var cur = start;
        while (cur < end - 0.05)
        {
            var chunkEnd = Math.Min(cur + maxLen, end);
            chunks.Add((cur, chunkEnd));
            cur = chunkEnd;
        }
        return chunks;
    }

    public static string GetOutputExtension(OutputFormat format)
    {
        return FormatPresets.Get(format).ext;
    }
}
