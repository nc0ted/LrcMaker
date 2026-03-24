namespace SyncAi.Blazor.Services;

public class LrcServiceSettings
{
    public long MaxAudioUploadSizeBytes => 20 * 1024 * 1024;
    public int GroqApiTimeoutMinutes => 5;
    public string WhisperModelName => "whisper-large-v3";
}
