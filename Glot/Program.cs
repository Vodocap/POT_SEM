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
using POT_SEM.Services.Patterns.Flyweight;
using POT_SEM.Services.Patterns.Flyweight.Cache;
using POT_SEM.Services.Patterns.ChainOfResponsibility.Translation;
using POT_SEM.Services.Dictionary;
using Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP CLIENT
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// SUPABASE CLIENT
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

// TOPIC GENERATION STRATEGIES
builder.Services.AddScoped<POT_SEM.Services.Patterns.Strategy.RandomWord.WikipediaRandomWordService>();
builder.Services.AddScoped<ITopicGenerationStrategy, POT_SEM.Services.Patterns.ChainOfResponsibility.TopicGeneration.ChainedTopicGenerationService>();

// LANGUAGE SOURCE FACTORY
builder.Services.AddScoped<LanguageSourceFactory>();

// TEXT SERVICES 

// Text cache service
builder.Services.AddSingleton<ITextCacheService, StoryCache>();

builder.Services.AddScoped<TextProviderFactory>();

builder.Services.AddSingleton<TextStorageService>();

// TRANSLATION SERVICES

builder.Services.AddScoped<ApiTranslationService>();
builder.Services.AddScoped<DatabaseTranslationService>();

// Register DictionaryTranslationStrategy
builder.Services.AddScoped<DictionaryTranslationStrategy>(sp =>
{
    var dictionaryService = sp.GetRequiredService<ApiDictionaryService>();
    return new DictionaryTranslationStrategy(dictionaryService);
});

builder.Services.AddScoped<ChainedTranslationService>(sp =>
{
    var apiService = sp.GetRequiredService<ApiTranslationService>();
    var dbService = sp.GetService<DatabaseTranslationService>();
    var cache = sp.GetRequiredService<WordFlyweightFactory>();
    var dictionary = sp.GetRequiredService<DictionaryTranslationStrategy>();

    return new ChainedTranslationService(cache, dictionary, dbService, apiService);
});

builder.Services.AddScoped<ITranslationStrategy>(sp => sp.GetRequiredService<ChainedTranslationService>());

// API Dictionary service
builder.Services.AddScoped(sp => new POT_SEM.Services.Dictionary.ApiDictionaryService(sp.GetRequiredService<HttpClient>()));

// Word Translation Cache (caches individual word translations)
builder.Services.AddScoped<WordFlyweightFactory>(sp =>
{
    var database = sp.GetService<DatabaseTranslationService>();
    return new WordFlyweightFactory(database);
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

// Processing facade
builder.Services.AddScoped<TextProcessingFacade>(sp =>
{
    var translationChain = sp.GetRequiredService<ITranslationStrategy>();
    var transliterationServices = sp.GetServices<POT_SEM.Core.Interfaces.ITransliterationService>();
    var furiganaEnrichment = sp.GetRequiredService<POT_SEM.Services.Transliteration.FuriganaEnrichmentService>();

    return new TextProcessingFacade(translationChain, transliterationServices, furiganaEnrichment);
});

await builder.Build().RunAsync();