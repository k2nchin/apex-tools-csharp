using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ApexTools.Core;

public static class FFmpegHelper
{
    private static string? _ffmpegPath;
    private static string? _ffprobePath;

    public static string FFmpeg => _ffmpegPath ??= FindExecutable("ffmpeg.exe", "ffmpeg");
    public static string FFprobe => _ffprobePath ??= FindExecutable("ffprobe.exe", "ffprobe");

    private static string FindExecutable(string winName, string unixName)
    {
        var exeName = Environment.OSVersion.Platform == PlatformID.Win32NT ? winName : unixName;

        // Check alongside the app first
        var appDir = AppContext.BaseDirectory;
        var local = Path.Combine(appDir, exeName);
        if (File.Exists(local)) return local;

        // Check in PATH
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            var full = Path.Combine(dir.Trim(), exeName);
            if (File.Exists(full)) return full;
        }

        return exeName; // fallback, let Process.Start fail naturally
    }

    public static async Task<double> GetDurationAsync(string videoPath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = FFprobe,
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi)!;
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            if (double.TryParse(output.Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var dur))
                return dur;
        }
        catch { }
        return 0;
    }

    public static async Task<(int w, int h)> GetSizeAsync(string videoPath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = FFprobe,
                Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of json \"{videoPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi)!;
            var json = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();

            var wMatch = Regex.Match(json, "\"width\":\\s*(\\d+)");
            var hMatch = Regex.Match(json, "\"height\":\\s*(\\d+)");
            if (wMatch.Success && hMatch.Success)
                return (int.Parse(wMatch.Groups[1].Value), int.Parse(hMatch.Groups[1].Value));
        }
        catch { }
        return (1920, 1080);
    }

    public static async Task<List<(double start, double end)>> DetectSilencesAsync(
        string videoPath, int noiseDb = -32, double minSilence = 0.7)
    {
        var silences = new List<(double, double)>();
        try
        {
            // Extract audio first
            var tempWav = Path.Combine(Path.GetTempPath(), "apex_analysis.wav");
            var psi = new ProcessStartInfo
            {
                FileName = FFmpeg,
                Arguments = $"-y -i \"{videoPath}\" -ac 1 -ar 16000 -vn \"{tempWav}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var proc = Process.Start(psi)!) { await proc.WaitForExitAsync(); }

            // Detect silences
            psi = new ProcessStartInfo
            {
                FileName = FFmpeg,
                Arguments = $"-hide_banner -i \"{tempWav}\" -af silencedetect=noise={noiseDb}dB:d={minSilence} -f null -",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var proc = Process.Start(psi)!)
            {
                var stderr = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                var starts = Regex.Matches(stderr, @"silence_start:\s*([\d.]+)");
                var ends = Regex.Matches(stderr, @"silence_end:\s*([\d.]+)");
                for (int i = 0; i < Math.Min(starts.Count, ends.Count); i++)
                {
                    if (double.TryParse(starts[i].Groups[1].Value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var s) &&
                        double.TryParse(ends[i].Groups[1].Value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var e))
                        silences.Add((s, e));
                }
            }

            try { File.Delete(tempWav); } catch { }
        }
        catch { }
        return silences;
    }

    public static async Task CutShortAsync(
        string videoPath, double start, double duration, string outputPath,
        Models.OutputFormat format, CancellationToken ct = default)
    {
        var (srcW, srcH) = await GetSizeAsync(videoPath);
        var cf = CropFilter(srcW, srcH);
        var fmt = Models.FormatPresets.Get(format);

        var fadeDur = Math.Min(1.5, duration * 0.6);
        var fadeStart = Math.Max(0.0, duration - fadeDur);

        var vf = $"{cf},scale=1080:1920:force_original_aspect_ratio=decrease," +
                 $"pad=1080:1920:(ow-iw)/2:(oh-ih)/2:color=black," +
                 $"fps=30,fade=t=out:st={fadeStart:F3}:d={fadeDur:F3},format=yuv420p";
        var af = $"afade=t=out:st={fadeStart:F3}:d={fadeDur:F3}";

        string args;
        if (fmt.vcodec.Contains("libvpx"))
        {
            args = $"-y -ss {start:F3} -i \"{videoPath}\" -t {duration:F3} " +
                   $"-vf \"{vf}\" -af \"{af}\" " +
                   $"-c:v {fmt.vcodec} -crf {fmt.crf} -b:v 0 " +
                   $"-c:a {fmt.acodec} -b:a {fmt.abit} -ar 44100 " +
                   $"-row-mt 1 \"{outputPath}\"";
        }
        else
        {
            args = $"-y -ss {start:F3} -i \"{videoPath}\" -t {duration:F3} " +
                   $"-vf \"{vf}\" -af \"{af}\" " +
                   $"-c:v {fmt.vcodec} -preset {fmt.preset} -crf {fmt.crf} " +
                   $"-c:a {fmt.acodec} -b:a {fmt.abit} -ar 44100 " +
                   $"-movflags +faststart \"{outputPath}\"";
        }

        var psi = new ProcessStartInfo
        {
            FileName = FFmpeg,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
        using var proc = Process.Start(psi)!;
        _ = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0)
            throw new Exception($"FFmpeg error cutting {Path.GetFileName(outputPath)}");
    }

    private static string CropFilter(int srcW, int srcH, double targetAr = 9.0 / 16.0)
    {
        if (srcW / (double)srcH > targetAr)
        {
            var outH = srcH;
            var outW = (int)Math.Round(srcH * targetAr);
            outW -= outW % 2;
            var x = (srcW - outW) / 2;
            x -= x % 2;
            return $"crop={outW}:{outH}:{x}:0";
        }
        else
        {
            var outW = srcW;
            var outH = (int)Math.Round(srcW / targetAr);
            outH -= outH % 2;
            var y = (srcH - outH) / 2;
            y -= y % 2;
            return $"crop={outW}:{outH}:0:{y}";
        }
    }
}
