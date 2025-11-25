using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Glot;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Builders;
using POT_SEM.Services;
using POT_SEM.Services.TextSources;
using POT_SEM.Services.TopicStrategies;
using POT_SEM.Services.RandomWordServices;
using POT_SEM.Services.Caching;
using POT_SEM.Services.Preloading;
using POT_SEM.Services.Database;
using POT_SEM.Services.Adapters;  // ✅ PRIDANÉ
using Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromSeconds(30)
});

// 🔑 LOAD SUPABASE CONFIG
Console.WriteLine("🔑 Loading Supabase configuration...");
var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var supabaseConfig = await SupabaseConfig.LoadAsync(http);
Console.WriteLine($"✅ Supabase URL: {supabaseConfig.Url}");

// 🔥 REGISTER SUPABASE CLIENT (Singleton)
builder.Services.AddSingleton(provider =>
{
    var options = new SupabaseOptions
    {
        AutoConnectRealtime = false,
        AutoRefreshToken = true
    };
    
    var client = new Client(supabaseConfig.Url, supabaseConfig.AnonKey, options);
    client.InitializeAsync().Wait();
    
    Console.WriteLine("✅ Supabase client initialized");
    return client;
});

// 💾 CACHE SERVICE
builder.Services.AddSingleton<ITextCacheService, TextCacheService>();

// 💿 STORAGE SERVICE
builder.Services.AddScoped<TextStorageService>();

// 🎲 Random Word Services
builder.Services.AddScoped<WikipediaRandomWordService>();
builder.Services.AddScoped<FallbackWordService>();

// 🎯 Topic Generation Strategy
builder.Services.AddScoped<ITopicGenerationStrategy, ApiTopicStrategy>();

// ✅ ADAPTER: IRandomWordService → ITopicGenerationStrategy
builder.Services.AddScoped<ITopicGenerationStrategy>(sp =>
    new RandomWordTopicAdapter(
        sp.GetRequiredService<WikipediaRandomWordService>()
    ));

// 🌉 LANGUAGE TEXT SOURCES
// Priority 1: Supabase (fast, cached)
builder.Services.AddScoped<ILanguageTextSource>(sp =>
    new SupabaseTextSource(
        sp.GetRequiredService<Client>(),
        "en",
        "English (Database)"
    ));

builder.Services.AddScoped<ILanguageTextSource>(sp =>
    new SupabaseTextSource(
        sp.GetRequiredService<Client>(),
        "sk",
        "Slovak (Database)"
    ));

// Priority 2: Wikipedia sources WITH AUTO-SAVE
// ✅ OPRAVENÉ: Použije RandomWordTopicAdapter
builder.Services.AddScoped<ILanguageTextSource>(sp =>
    new AutoSaveTextSourceWrapper(
        new EnglishTextSource(
            sp.GetRequiredService<HttpClient>(),
            new RandomWordTopicAdapter(sp.GetRequiredService<WikipediaRandomWordService>())
        ),
        sp.GetRequiredService<TextStorageService>()
    ));

builder.Services.AddScoped<ILanguageTextSource>(sp =>
    new AutoSaveTextSourceWrapper(
        new SlovakTextSource(
            sp.GetRequiredService<HttpClient>(),
            new RandomWordTopicAdapter(sp.GetRequiredService<WikipediaRandomWordService>())
        ),
        sp.GetRequiredService<TextStorageService>()
    ));

builder.Services.AddScoped<ILanguageTextSource>(sp =>
    new AutoSaveTextSourceWrapper(
        new ArabicTextSource(
            sp.GetRequiredService<HttpClient>(),
            new RandomWordTopicAdapter(sp.GetRequiredService<WikipediaRandomWordService>())
        ),
        sp.GetRequiredService<TextStorageService>()
    ));

builder.Services.AddScoped<ILanguageTextSource>(sp =>
    new AutoSaveTextSourceWrapper(
        new JapaneseTextSource(
            sp.GetRequiredService<HttpClient>(),
            new RandomWordTopicAdapter(sp.GetRequiredService<WikipediaRandomWordService>())
        ),
        sp.GetRequiredService<TextStorageService>()
    ));

// 🏗️ Text Provider Builder
builder.Services.AddScoped<TextProviderBuilder>();

// 🚀 Preload Service
builder.Services.AddScoped<TextPreloadService>();

var app = builder.Build();

// 🔥 BACKGROUND PRELOAD
Console.WriteLine("=== APPLICATION STARTING ===");

_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(1000);
        
        using (var scope = app.Services.CreateScope())
        {
            var preloadService = scope.ServiceProvider.GetRequiredService<TextPreloadService>();
            await preloadService.PreloadAllAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Background preload failed: {ex.Message}");
    }
});

Console.WriteLine("=== APPLICATION READY ===");

await app.RunAsync();