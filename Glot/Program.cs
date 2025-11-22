using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Glot;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Builders;
using POT_SEM.Services.TextSources;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// Language Sources s debug výpismi
Console.WriteLine("🔧 Registering language sources...");

builder.Services.AddScoped<ILanguageTextSource, EnglishTextSource>();
Console.WriteLine("   ✅ EnglishTextSource registered");

builder.Services.AddScoped<ILanguageTextSource, ArabicTextSource>();
Console.WriteLine("   ✅ ArabicTextSource registered");

builder.Services.AddScoped<ILanguageTextSource, SlovakTextSource>();
Console.WriteLine("   ✅ SlovakTextSource registered");
builder.Services.AddScoped<ILanguageTextSource, JapaneseTextSource>();
Console.WriteLine("   ✅ JapaneseTextSource registered");

// Builder
builder.Services.AddScoped<TextProviderBuilder>();
Console.WriteLine("   ✅ TextProviderBuilder registered");

Console.WriteLine("🚀 Starting application...");

var app = builder.Build();

// DEBUG: Otestuj DI po build
using (var scope = app.Services.CreateScope())
{
    var sources = scope.ServiceProvider.GetServices<ILanguageTextSource>();
    var sourcesList = sources.ToList();
    
    Console.WriteLine($"🔍 DEBUG: Found {sourcesList.Count} language sources:");
    
    foreach (var source in sourcesList)
    {
        Console.WriteLine($"   • {source.LanguageName} ({source.LanguageCode})");
    }
    
    if (sourcesList.Count == 0)
    {
        Console.WriteLine("❌ WARNING: No language sources found in DI!");
    }
}

await app.RunAsync();