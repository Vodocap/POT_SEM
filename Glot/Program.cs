using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Glot;
using Microsoft.JSInterop;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.Factory;
using POT_SEM.Services.Databases;
using POT_SEM.Services.Patterns.Facade;
using POT_SEM.Services.Patterns.Strategy;
using POT_SEM.Services.Patterns.Strategy.Topic;
using POT_SEM.Services.Patterns.Flyweight;
using POT_SEM.Services.Patterns.Flyweight.Cache;
using POT_SEM.Services.Patterns.ChainOfResponsibility;
using POT_SEM.Services.Patterns.Composite;
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

// Register DictionaryTranslationStrategy (no helper needed initially)
builder.Services.AddScoped<DictionaryTranslationStrategy>(sp =>
{
    var cache = sp.GetRequiredService<TranslationCacheService>();
    // Don't pass helper to avoid circular dependency - meanings joining is simple enough
    return new DictionaryTranslationStrategy(cache, null);
});

// CHAIN OF RESPONSIBILITY: Cache → Database → Dictionary → API
builder.Services.AddScoped<ChainedTranslationService>(sp =>
{
    var apiService = sp.GetRequiredService<ApiTranslationService>();
    var dbService = sp.GetService<DatabaseTranslationService>();
    var cache = sp.GetRequiredService<TranslationCacheService>();
    var dictionary = sp.GetRequiredService<DictionaryTranslationStrategy>();

    // Create chain: Cache → DB (fast) → Dictionary (slow AI) → API (slowest)
    return new ChainedTranslationService(cache, dictionary, dbService, apiService);
});

// Register the chain as ITranslationStrategy
builder.Services.AddScoped<ITranslationStrategy>(sp => sp.GetRequiredService<ChainedTranslationService>());

// Register DictionaryTranslationHelper (uses ChainedTranslationService for DB persistence)
builder.Services.AddScoped<POT_SEM.Services.Dictionary.DictionaryTranslationHelper>(sp =>
{
    var chainedService = sp.GetService<ChainedTranslationService>();
    return new POT_SEM.Services.Dictionary.DictionaryTranslationHelper(chainedService);
});

// API Dictionary service
builder.Services.AddScoped(sp => new POT_SEM.Services.Dictionary.ApiDictionaryService(sp.GetRequiredService<HttpClient>()));

// Translation Cache Service (simple memoization)
builder.Services.AddScoped<TranslationCacheService>(sp =>
{
    var apiDict = sp.GetService<POT_SEM.Services.Dictionary.ApiDictionaryService>();
    return apiDict != null ? new TranslationCacheService(apiDict) : new TranslationCacheService();
});

// FLYWEIGHT PATTERN (Gang of Four) - Word Flyweight Factory with database integration
builder.Services.AddScoped<WordFlyweightFactory>(sp =>
{
    var database = sp.GetService<DatabaseTranslationService>();
    var apiDict = sp.GetService<POT_SEM.Services.Dictionary.ApiDictionaryService>();
    return new WordFlyweightFactory(database, apiDict);
});

// Transliteration services (Arabic, Japanese)
builder.Services.AddSingleton<POT_SEM.Services.Transliteration.ArabicTransliterationService>();
builder.Services.AddSingleton<POT_SEM.Services.Transliteration.JapaneseRomajiService>();
// Register transliteration implementations for IEnumerable<ITransliterationService>
builder.Services.AddSingleton<POT_SEM.Core.Interfaces.ITransliterationService, POT_SEM.Services.Transliteration.ArabicTransliterationService>();
builder.Services.AddSingleton<POT_SEM.Core.Interfaces.ITransliterationService, POT_SEM.Services.Transliteration.JapaneseRomajiService>();

// Furigana enrichment service with API integration
builder.Services.AddScoped<POT_SEM.Services.Transliteration.FuriganaEnrichmentService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var romajiService = sp.GetRequiredService<POT_SEM.Services.Transliteration.JapaneseRomajiService>();
    return new POT_SEM.Services.Transliteration.FuriganaEnrichmentService(httpClient, romajiService);
});
// Also register as ITransliterationService
builder.Services.AddScoped<POT_SEM.Core.Interfaces.ITransliterationService>(sp => 
    sp.GetRequiredService<POT_SEM.Services.Transliteration.FuriganaEnrichmentService>());

// ========================================
// PROCESSING SERVICES (COMPOSITE + FACADE)
// ========================================

// Processing facade (simplifies the whole pipeline)
builder.Services.AddScoped<TextProcessingFacade>(sp =>
{
    var translationChain = sp.GetRequiredService<ITranslationStrategy>();
    var transliterationServices = sp.GetServices<POT_SEM.Core.Interfaces.ITransliterationService>();
    var cache = sp.GetRequiredService<TranslationCacheService>();
    var furiganaEnrichment = sp.GetRequiredService<POT_SEM.Services.Transliteration.FuriganaEnrichmentService>();

    return new TextProcessingFacade(translationChain, transliterationServices, cache, furiganaEnrichment);
});

// ========================================
// RUN APP
// ========================================

await builder.Build().RunAsync();