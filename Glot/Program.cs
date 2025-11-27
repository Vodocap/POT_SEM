using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Glot;
using POT_SEM.Core.Interfaces;
using POT_SEM.Services.Builders;
using POT_SEM.Services.Caching;
using POT_SEM.Services.Database;
using POT_SEM.Services.Factories;
using POT_SEM.Services.Preloading;
using POT_SEM.Services.RandomWordServices;
using POT_SEM.Services.TopicStrategies;
using Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ========================================
// HTTP CLIENT
// ========================================
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// ========================================
// SUPABASE CLIENT (Optional Dependency)
// ========================================
Client? supabaseClient = null;
try
{
    var httpForConfig = new HttpClient 
    { 
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
    };
    
    var config = await SupabaseConfig.LoadAsync(httpForConfig);
    supabaseClient = new Client(config.Url, config.AnonKey);
    await supabaseClient.InitializeAsync();
    
    builder.Services.AddSingleton(supabaseClient);
    Console.WriteLine("✅ Supabase initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Supabase unavailable: {ex.Message}");
    Console.WriteLine("ℹ️ Application will run without database support");
}

// ========================================
// RANDOM WORD SERVICES
// Strategy Pattern - Different word generation implementations
// ========================================
builder.Services.AddScoped<WikipediaRandomWordService>();
builder.Services.AddScoped<FallbackWordService>();

// ========================================
// TOPIC GENERATION STRATEGIES
// Strategy Pattern - Different topic generation approaches
// ========================================
builder.Services.AddScoped<StaticTopicStrategy>();
builder.Services.AddScoped<ApiTopicStrategy>();

if (supabaseClient != null)
{
    builder.Services.AddScoped<DatabaseTopicStrategy>();
}

// Default strategy: API-based with Wikipedia
builder.Services.AddScoped<ITopicGenerationStrategy>(sp => 
    sp.GetRequiredService<ApiTopicStrategy>());

// ========================================
// LANGUAGE SOURCE FACTORY
// Factory + Template Method Pattern
// ========================================
builder.Services.AddScoped<LanguageSourceFactory>();

// ========================================
// TEXT CACHE SERVICE
// Singleton - Shared cache across application
// ========================================
builder.Services.AddSingleton<ITextCacheService, TextCacheService>();

// ========================================
// TEXT STORAGE SERVICE (Database-dependent)
// ========================================
if (supabaseClient != null)
{
    builder.Services.AddScoped<TextStorageService>();
}

// ========================================
// TEXT PROVIDER BUILDER
// Builder + Fluent API Pattern
// Auto-wired dependencies via constructor injection
// ========================================
builder.Services.AddScoped<TextProviderBuilder>();

// ========================================
// TEXT PRELOAD SERVICE
// Eager initialization for cache warming
// ========================================
builder.Services.AddScoped<TextPreloadService>();

// ========================================
// RUN APPLICATION
// ========================================
var app = builder.Build();

Console.WriteLine("🚀 Application initialized");
Console.WriteLine($"📦 Services registered:");
Console.WriteLine($"   - Supabase: {(supabaseClient != null ? "✅" : "❌")}");
Console.WriteLine($"   - Text Cache: ✅");
Console.WriteLine($"   - Language Factory: ✅");
Console.WriteLine($"   - Topic Strategy: ✅ (API-based)");

await app.RunAsync();