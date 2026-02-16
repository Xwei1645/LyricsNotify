using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LyricsNotify.Models;

namespace LyricsNotify.Helpers;

public static class LrcParser
{
    private static readonly Regex LrcRegex = new(@"\[(?<time>\d+:\d+(\.\d+)?)\]", RegexOptions.Compiled);
    private static readonly Regex MetadataRegex = new(@"\[(?<key>[a-z]+):(?<value>.*)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static List<LrcLine> Parse(string lrcContent)
    {
        // 保留所有行（包括空行），以便处理带时间戳但内容为空的行
        var lines = lrcContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var result = new List<LrcLine>();
        var offset = TimeSpan.Zero;

        foreach (var line in lines)
        {
            // 跳过完全空白的行
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var metadataMatch = MetadataRegex.Match(line);
            if (metadataMatch.Success)
            {
                var key = metadataMatch.Groups["key"].Value.ToLower();
                var value = metadataMatch.Groups["value"].Value;
                if (key == "offset" && int.TryParse(value, out var ms))
                {
                    offset = TimeSpan.FromMilliseconds(ms);
                }
                continue;
            }

            var matches = LrcRegex.Matches(line);
            if (matches.Count == 0) continue;

            var content = line;
            foreach (Match match in matches)
            {
                content = content.Replace(match.Value, "");
            }
            content = content.Trim();

            foreach (Match match in matches)
            {
                var timeStr = match.Groups["time"].Value;
                if (TryParseTime(timeStr, out var time))
                {
                    result.Add(new LrcLine { Time = time + offset, Content = content });
                }
            }
        }

        var parsed = result.OrderBy(l => l.Time).ToList();
        var merged = new List<LrcLine>();
        foreach (var line in parsed)
        {
            if (merged.Count > 0 && merged[^1].Time == line.Time)
            {
                merged[^1] = new LrcLine
                {
                    Time = line.Time,
                    Content = merged[^1].Content + " / " + line.Content
                };
            }
            else
            {
                merged.Add(line);
            }
        }

        return merged;
    }

    private static bool TryParseTime(string timeStr, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        var parts = timeStr.Split(':');
        if (parts.Length != 2) return false;

        if (!double.TryParse(parts[0], out var minutes)) return false;
        if (!double.TryParse(parts[1], out var seconds)) return false;

        time = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        return true;
    }
}
