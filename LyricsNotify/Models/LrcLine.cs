using System;

namespace LyricsNotify.Models;

public class LrcLine
{
    public TimeSpan Time { get; set; }
    public string Content { get; set; } = string.Empty;
}
