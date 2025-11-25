using POT_SEM.Core.Models;
using POT_SEM.Core.Builders;
using POT_SEM.Services.Caching;
using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Preloading
{
    /// <summary>
    /// Preloads texts at application startup
    /// </summary>
    public class TextPreloadService
    {
        private readonly TextProviderBuilder _builder;
        private readonly ITextCacheService _cache;        
        // Configuration
        private const int TEXTS_PER_DIFFICULTY = 5; // Preload 10 texts per difficulty
        
        private static readonly string[] SupportedLanguages = { "en", "sk", "ar", "ja" };
        private static readonly DifficultyLevel[] SupportedDifficulties = 
        {
            DifficultyLevel.Beginner,
            DifficultyLevel.Intermediate,
            DifficultyLevel.Advanced
        };
        
        public TextPreloadService(TextProviderBuilder builder, ITextCacheService cache)
        {
            _builder = builder;
            _cache = cache;
        }
        
        /// <summary>
        /// Preload all combinations - PARALLEL for speed!
        /// </summary>
        public async Task PreloadAllAsync()
        {
            Console.WriteLine("🚀 Starting text preload...");
            var startTime = DateTime.UtcNow;
            
            var tasks = new List<Task>();
            
            foreach (var lang in SupportedLanguages)
            {
                foreach (var difficulty in SupportedDifficulties)
                {
                    // Create parallel tasks
                    tasks.Add(PreloadSingleAsync(lang, difficulty));
                }
            }
            
            // Wait for all to complete
            await Task.WhenAll(tasks);
            
            var elapsed = DateTime.UtcNow - startTime;
            var stats = _cache.GetStats();
            
            Console.WriteLine($"✅ Preload complete in {elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"   📊 Cached {stats.TotalCachedTexts} texts across {stats.CachedLanguages} languages");
        }
        
        /// <summary>
        /// Preload specific language + difficulty
        /// </summary>
        private async Task PreloadSingleAsync(string languageCode, DifficultyLevel difficulty)
        {
            try
            {
                Console.WriteLine($"⏳ Preloading {languageCode} - {difficulty}...");
                
                var provider = _builder
                    .ForLanguage(languageCode)
                    .ForDifficulty(difficulty)
                    .Build();
                
                var texts = await provider.GetTextsAsync(count: TEXTS_PER_DIFFICULTY);
                
                if (texts.Any())
                {
                    _cache.CacheTexts(languageCode, difficulty, texts);
                    Console.WriteLine($"   ✅ {languageCode} - {difficulty}: {texts.Count} texts");
                }
                else
                {
                    Console.WriteLine($"   ⚠️ {languageCode} - {difficulty}: No texts fetched");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ {languageCode} - {difficulty}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Preload only specific language (faster)
        /// </summary>
        public async Task PreloadLanguageAsync(string languageCode)
        {
            Console.WriteLine($"🚀 Preloading {languageCode}...");
            
            var tasks = SupportedDifficulties
                .Select(difficulty => PreloadSingleAsync(languageCode, difficulty))
                .ToList();
            
            await Task.WhenAll(tasks);
            
            Console.WriteLine($"✅ {languageCode} preload complete");
        }
    }
}