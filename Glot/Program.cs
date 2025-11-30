using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Glot;
using Microsoft.JSInterop;
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

// Register DictionaryTranslationStrategy (no helper needed initially)
builder.Services.AddScoped<DictionaryTranslationStrategy>(sp =>
{
    var flyweight = sp.GetRequiredService<TranslationFlyweightFactory>();
    // Don't pass helper to avoid circular dependency - meanings joining is simple enough
    return new DictionaryTranslationStrategy(flyweight, null);
});

// CHAIN OF RESPONSIBILITY: Flyweight → Dictionary → Database → API
builder.Services.AddScoped<ChainedTranslationService>(sp =>
{
    var apiService = sp.GetRequiredService<ApiTranslationService>();
    var dbService = sp.GetService<DatabaseTranslationService>();
    var flyweight = sp.GetRequiredService<TranslationFlyweightFactory>();
    var dictionary = sp.GetRequiredService<DictionaryTranslationStrategy>();

    // Create chain: Flyweight → Dictionary → DB (optional) → API
    return new ChainedTranslationService(flyweight, dictionary, dbService, apiService);
});

// Register the chain as ITranslationStrategy
builder.Services.AddScoped<ITranslationStrategy>(sp => sp.GetRequiredService<ChainedTranslationService>());

// Register DictionaryTranslationHelper (uses ChainedTranslationService for DB persistence)
builder.Services.AddScoped<POT_SEM.Services.Dictionary.DictionaryTranslationHelper>(sp =>
{
    var chainedService = sp.GetService<ChainedTranslationService>();
    return new POT_SEM.Services.Dictionary.DictionaryTranslationHelper(chainedService);
});

// Wiktionary service and Translation flyweight factory (now includes dictionary cache)
builder.Services.AddScoped(sp => new POT_SEM.Services.Dictionary.WiktionaryService(sp.GetRequiredService<HttpClient>()));
builder.Services.AddScoped<TranslationFlyweightFactory>(sp =>
{
    var wiki = sp.GetService<POT_SEM.Services.Dictionary.WiktionaryService>();
    return wiki != null ? new TranslationFlyweightFactory(wiki) : new TranslationFlyweightFactory();
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

// Text parser (COMPOSITE pattern)
builder.Services.AddScoped<TextParser>();

// Processing facade (simplifies the whole pipeline)
builder.Services.AddScoped<TextProcessingFacade>(sp =>
{
    var parser = sp.GetRequiredService<TextParser>();
    var translationChain = sp.GetRequiredService<ITranslationStrategy>();
    var transliterationServices = sp.GetServices<POT_SEM.Core.Interfaces.ITransliterationService>();
    var flyweight = sp.GetRequiredService<TranslationFlyweightFactory>();
    var furiganaEnrichment = sp.GetRequiredService<POT_SEM.Services.Transliteration.FuriganaEnrichmentService>();

    return new TextProcessingFacade(parser, translationChain, transliterationServices, flyweight, furiganaEnrichment);
});

// ========================================
// RUN APP
// ========================================

await builder.Build().RunAsync();