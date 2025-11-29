using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Glot;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Builders;
using POT_SEM.Services.Database;
using POT_SEM.Services.Processing;
using POT_SEM.Services.Translation;
using POT_SEM.Services.Factories;
using POT_SEM.Services.TopicStrategies;
using POT_SEM.Services.Caching;
using Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ========================================
// HTTP CLIENT (pre API calls)
// ========================================
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// ========================================
// SUPABASE CLIENT
// ========================================
// Use an HttpClient with BaseAddress set so relative config file requests work in WASM
var httpForConfig = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var supabaseConfig = await SupabaseConfig.LoadAsync(httpForConfig);

builder.Services.AddSingleton(provider => 
{
    var url = supabaseConfig.Url;
    var key = supabaseConfig.AnonKey;
    
    var options = new Supabase.SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = true
    };
    
    return new Supabase.Client(url, key, options);
});

// ========================================
// TOPIC GENERATION STRATEGIES
// ========================================
builder.Services.AddSingleton<ITopicGenerationStrategy, StaticTopicStrategy>();

// ========================================
// LANGUAGE SOURCE FACTORY
// ========================================
// Register as scoped so it can consume scoped services like HttpClient
builder.Services.AddScoped<LanguageSourceFactory>();

// ========================================
// TEXT SERVICES (from existing system)
// ========================================

// Text cache service
builder.Services.AddSingleton<ITextCacheService, TextCacheService>();

// Text provider builder
// Register as scoped to allow dependencies that are scoped (HttpClient, factory)
builder.Services.AddScoped<TextProviderBuilder>();

// Text storage
builder.Services.AddSingleton<TextStorageService>();

// ========================================
// TRANSLATION SERVICES (STRATEGY PATTERN)
// ========================================

// Register all translation strategies
builder.Services.AddScoped<ApiTranslationService>();
builder.Services.AddScoped<DatabaseTranslationService>();

// CHAIN OF RESPONSIBILITY: Database → API
builder.Services.AddScoped<ITranslationStrategy>(sp =>
{
    var apiService = sp.GetRequiredService<ApiTranslationService>();
    var dbService = sp.GetService<DatabaseTranslationService>();
    var flyweight = sp.GetRequiredService<TranslationFlyweightFactory>();

    // Create chain: Flyweight → DB (optional) → API
    var chain = new ChainedTranslationService(flyweight, dbService, apiService);

    return chain;
});

// Translation flyweight factory
builder.Services.AddSingleton<TranslationFlyweightFactory>();

// ========================================
// PROCESSING SERVICES (COMPOSITE + FACADE)
// ========================================

// Text parser (COMPOSITE pattern)
builder.Services.AddScoped<TextParser>();

// Processing facade (simplifies the whole pipeline)
builder.Services.AddScoped<TextProcessingFacade>();
// Furigana decorator (MVP static dictionary)
builder.Services.AddSingleton<POT_SEM.Services.Decorators.FuriganaDecorator>();
// Heuristic fallback decorator (server-side, coarse readings)
builder.Services.AddSingleton<POT_SEM.Services.Decorators.HeuristicFuriganaDecorator>();
// Note: Kuroshiro client removed; server-side heuristic/fallback decorators are used instead.

// ========================================
// RUN APP
// ========================================

await builder.Build().RunAsync();