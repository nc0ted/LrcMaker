using System.Text;

namespace SyncAi.Blazor.Services.Lrc;

public class LrcAligner
{
    private void Log(string msg, List<string> logs)
        => logs.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");

    public string AlignLrc(string rawLrc, string cleanLyrics, List<string> logs, double audioDuration)
    {
        var originalLines = cleanLyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();
        Log($"[ALIGN] Original lines: {originalLines.Count}", logs);

        var draftSegments = new List<(TimeSpan Start, string Text)>();
        foreach (var line in rawLrc.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith('['))
            {
                var parts = line.Split(']', 2);
                if (parts.Length == 2 && TimeSpan.TryParseExact(parts[0].TrimStart('['), @"mm\:ss\.ff", null, out var start))
                {
                    draftSegments.Add((start, parts[1].Trim()));
                }
            }
        }
        Log($"[ALIGN] Draft segments: {draftSegments.Count}", logs);

        if (draftSegments.Count == 0)
        {
            var sb = new StringBuilder();
            var currentTime = TimeSpan.Zero;
            var fallbackIncrement = TimeSpan.FromSeconds(3.5);
            foreach (var line in originalLines)
            {
                sb.AppendLine($@"[{currentTime:mm\:ss\.ff}] {line}");
                currentTime += fallbackIncrement;
            }
            return sb.ToString().Trim();
        }

        double avgDurationSecs = 3.5;
        if (draftSegments.Count > 1)
        {
            double totalDiff = 0;
            for (int i = 1; i < draftSegments.Count; i++)
            {
                totalDiff += (draftSegments[i].Start - draftSegments[i - 1].Start).TotalSeconds;
            }
            avgDurationSecs = totalDiff / (draftSegments.Count - 1);
            avgDurationSecs = Math.Max(2.0, avgDurationSecs);
        }
        Log($"[ALIGN] Avg line duration: {avgDurationSecs:F2} secs", logs);

        var maxDuration = TimeSpan.FromSeconds(audioDuration > 0 ? audioDuration + 30 : 600);

        var result = new List<string>();
        int draftIndex = 0;
        var lastAssignedTime = draftSegments.Count > 0 ? draftSegments[0].Start : TimeSpan.Zero;
        lastAssignedTime -= TimeSpan.FromSeconds(1.5);

        var increment = TimeSpan.FromSeconds(avgDurationSecs);
        var minIncrement = TimeSpan.FromSeconds(1.0);
        const double matchThreshold = 0.62;

        foreach (var orig in originalLines)
        {
            TimeSpan candidateTime = lastAssignedTime + increment;
            bool matched = false;

            for (int j = draftIndex; j < Math.Min(draftIndex + 12, draftSegments.Count); j++)
            {
                var draftText = draftSegments[j].Text;
                double similarity = NormalizedLevenshtein(orig, draftText);

                if (similarity >= matchThreshold)
                {
                    candidateTime = draftSegments[j].Start;

                    if (candidateTime <= lastAssignedTime)
                    {
                        candidateTime = lastAssignedTime + minIncrement;
                    }

                    lastAssignedTime = candidateTime;
                    draftIndex = j + 1;
                    matched = true;

                    Log($"[ALIGN] Matched '{orig.Substring(0, Math.Min(30, orig.Length))}'... → sim {similarity:F2} at [{candidateTime:mm\\:ss\\.ff}]", logs);
                    break;
                }
            }

            if (!matched)
            {
                candidateTime = lastAssignedTime + increment;
                if (candidateTime < lastAssignedTime + minIncrement)
                    candidateTime = lastAssignedTime + minIncrement;

                Log($"[ALIGN] No match for '{orig.Substring(0, Math.Min(30, orig.Length))}'... estimate [{candidateTime:mm\\:ss\\.ff}]", logs);
            }

            if (candidateTime > maxDuration)
            {
                Log($"[ALIGN] Reached end of audio ({maxDuration:mm\\:ss\\.ff}), stopping alignment for remaining lines", logs);
                break;
            }

            while (result.Any(r => r.StartsWith($"[{candidateTime:mm\\:ss\\.ff}]")))
            {
                candidateTime += minIncrement;
            }

            result.Add($"[{candidateTime:mm\\:ss\\.ff}] {orig}");
            lastAssignedTime = candidateTime;
        }

        var finalLrc = string.Join("\n", result);
        Log("[ALIGN] RESULT PREVIEW:\n" + finalLrc.Substring(0, Math.Min(800, finalLrc.Length)), logs);

        return finalLrc;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        s1 = s1.ToLowerInvariant();
        s2 = s2.ToLowerInvariant();
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    private static double NormalizedLevenshtein(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 1.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;

        int dist = LevenshteinDistance(s1, s2);
        return 1.0 - (double)dist / Math.Max(s1.Length, s2.Length);
    }
}
