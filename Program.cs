using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SyncAi.Blazor.Components;
using SyncAi.Blazor.Services;
using SyncAi.Blazor.Services.Lrc;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<LrcService>();
builder.Services.AddScoped<WhisperTranscriptionService>();
builder.Services.AddScoped<LrcAligner>();
builder.Services.Configure<LrcServiceSettings>(_ => { });

await builder.Build().RunAsync();
