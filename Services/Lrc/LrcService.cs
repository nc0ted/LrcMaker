namespace SyncAi.Blazor.Services.Lrc;

public class LrcService(ILogger<LrcService> logger, WhisperTranscriptionService transcriptionService,
    LrcAligner aligner)
{
    public async Task<(string? lrcContent, List<string> logs)> ProcessLrc(string lyricsText, Stream audioStream, string audioFileName, string groqApiKey)
    {
        var logs = new List<string>();

        try
        {
            logger.LogInformation("ProcessLrc started for {FileName}", audioFileName);
            logs.Add($"[{DateTime.Now:HH:mm:ss}] Processing request...");

            logs.Add($"[{DateTime.Now:HH:mm:ss}] Transcribing audio...");

            var (rawLrc, audioDuration) = await transcriptionService.TranscribeAudio(audioStream, audioFileName, groqApiKey);

            logs.Add($"[{DateTime.Now:HH:mm:ss}] Aligning lyrics...");
            string finalLrc = aligner.AlignLrc(rawLrc, lyricsText, logs, audioDuration);

            logs.Add($"[{DateTime.Now:HH:mm:ss}] Generation complete.");
            logger.LogInformation("ProcessLrc for {FileName} completed successfully.", audioFileName);

            return (finalLrc, logs);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Processing cancelled due to timeout for ProcessLrc for {FileName}", audioFileName);
            logs.Add($"[{DateTime.Now:HH:mm:ss}] Processing cancelled due to timeout");
            return (null, logs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during ProcessLrc for {FileName}", audioFileName);
            logs.Add($"[{DateTime.Now:HH:mm:ss}] !!! ERROR: {ex.Message}");
            return (null, logs);
        }
    }
}