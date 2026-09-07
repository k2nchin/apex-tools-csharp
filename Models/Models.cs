namespace ApexTools.Models;

public class SongSegment
{
    public int Index { get; set; }
    public double Start { get; set; }
    public double End { get; set; }
    public string Artist { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "pending"; // pending, analyzing, ready, processing, done, error
    public string? Error { get; set; }

    public double Duration => End - Start;

    public SongSegment Clone() => new()
    {
        Index = Index, Start = Start, End = End,
        Artist = Artist, Title = Title, Status = Status
    };
}

public class VideoInfo
{
    public string Path { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Title { get; set; } = "";
    public double Duration { get; set; }
}

public class QueueItem
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "pending";
    public List<SongSegment>? Songs { get; set; }
    public string? Error { get; set; }
    public string? Result { get; set; }
    public double Duration { get; set; }
}

public enum OutputFormat
{
    Mp4High,   // CRF 18
    Mp4Med,    // CRF 23
    Mp4Low,    // CRF 28
    WebmHigh,  // CRF 24
    WebmMed    // CRF 32
}

public static class FormatPresets
{
    public static (string vcodec, string preset, string crf, string ext, string acodec, string abit) Get(OutputFormat fmt) => fmt switch
    {
        OutputFormat.Mp4High  => ("libx264",  "medium", "18", "mp4", "aac",  "192k"),
        OutputFormat.Mp4Med   => ("libx264",  "medium", "23", "mp4", "aac",  "192k"),
        OutputFormat.Mp4Low   => ("libx264",  "fast",   "28", "mp4", "aac",  "128k"),
        OutputFormat.WebmHigh => ("libvpx-vp9","",      "24", "webm","libopus","128k"),
        OutputFormat.WebmMed  => ("libvpx-vp9","",      "32", "webm","libopus","96k"),
        _ => ("libx264", "medium", "23", "mp4", "aac", "192k")
    };
}
