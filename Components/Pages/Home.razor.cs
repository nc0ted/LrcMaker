using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using SyncAi.Blazor.Services;
using SyncAi.Blazor.Services.Lrc;

namespace SyncAi.Blazor.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private LrcService LrcService { get; set; } = default!;
    [Inject] private ILogger<Home> Logger { get; set; } = default!;
    [Inject] private IOptions<LrcServiceSettings> LrcSettingsOptions { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private LrcServiceSettings _lrcSettings = default!;

    private string LyricsText = "";
    private IBrowserFile? SelectedFile;
    private List<string> Logs = new();
    private bool IsBusy = false;
    private string? DownloadUrl;

    private string? GroqApiKey { get; set; }
    private string? ApiKeyStatusMessage { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _lrcSettings = LrcSettingsOptions.Value;
            var key = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "groqApiKey");
            if (!string.IsNullOrWhiteSpace(key))
            {
                GroqApiKey = key;
                StateHasChanged();
            }
        }
    }

    private async Task UpdateApiKey()
    {
        if (!string.IsNullOrWhiteSpace(GroqApiKey))
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "groqApiKey", GroqApiKey);
            ApiKeyStatusMessage = "API Key saved in local storage!";

            await Task.Delay(3000);
            ApiKeyStatusMessage = null;
            StateHasChanged();
        }
    }

    private void OnInputFileChange(InputFileChangeEventArgs e) => SelectedFile = e.File;

    private async Task Process()
    {
        if (SelectedFile == null || string.IsNullOrWhiteSpace(LyricsText)) return;

        if (string.IsNullOrWhiteSpace(GroqApiKey))
        {
            Logs.Add("!!! Please enter and save your Groq API Key first.");
            return;
        }

        if (LyricsText.Length > 10000)
        {
            Logs.Add("!!! Lyrics text is too long. Maximum allowed length is 10,000 characters.");
            return;
        }

        var allowedExtensions = new[] { ".mp3", ".wav", ".m4a", ".mp4", ".webm", ".mpeg", ".mpga" };
        var fileExt = Path.GetExtension(SelectedFile.Name).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExt))
        {
            Logs.Add("!!! Invalid file type. Only audio files are allowed (.mp3, .wav, .m4a, .mp4,.webm, .mpeg, .mpga).");
            return;
        }

        IsBusy = true;
        Logs.Clear();
        DownloadUrl = null;

        try
        {
            using var stream = SelectedFile.OpenReadStream(_lrcSettings.MaxAudioUploadSizeBytes);
            var (lrcContent, serviceLogs) = await LrcService.ProcessLrc(LyricsText, stream, SelectedFile.Name, GroqApiKey);

            Logs.AddRange(serviceLogs);

            if (lrcContent != null)
            {
                DownloadUrl = $"data:application/octet-stream;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(lrcContent))}";
            }
        }
        catch (Exception ex)
        {
            Logs.Add($"!!! An unexpected error occurred during processing. Please try again.");
            Logger.LogError(ex, "An unexpected error occurred during LRC processing.");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
