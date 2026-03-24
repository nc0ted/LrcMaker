using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SyncAi.Blazor.Services.Lrc;

public class WhisperTranscriptionService(IOptions<LrcServiceSettings> settings, HttpClient httpClient)
{
    private readonly LrcServiceSettings _settings = settings.Value;

    public async Task<(string rawLrc, double duration)> TranscribeAudio(Stream audioStream, string fileName, string apiKey)
    {
        httpClient.Timeout = TimeSpan.FromMinutes(_settings.GroqApiTimeoutMinutes);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(audioStream);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".mp4" => "audio/mp4",
            ".webm" => "audio/webm",
            _ => "audio/mpeg"
        };
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);

        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(_settings.WhisperModelName), "model");
        content.Add(new StringContent("verbose_json"), "response_format");

        var resp = await httpClient.PostAsync("https://api.groq.com/openai/v1/audio/transcriptions", content);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Groq Error: {body}");

        using var doc = JsonDocument.Parse(body);
        var segments = doc.RootElement.GetProperty("segments").EnumerateArray();

        var sb = new StringBuilder();
        foreach (var seg in segments)
        {
            var text = seg.GetProperty("text").GetString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var start = TimeSpan.FromSeconds(seg.GetProperty("start").GetDouble());
            sb.AppendLine($"[{start:mm\\:ss\\.ff}] {text}");
        }

        double duration = doc.RootElement.TryGetProperty("duration", out var durElement) ? durElement.GetDouble() : 0;

        return (sb.ToString().Trim(), duration);
    }
}
