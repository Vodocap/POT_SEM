using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using POT_SEM.Services.Builders;

namespace POT_SEM.Services.Preloading
{
    public class TextPreloadService
    {
        private readonly TextProviderBuilder _builder;
        private readonly ITextCacheService _cache;

        public TextPreloadService(
            TextProviderBuilder builder,
            ITextCacheService cache)
        {
            _builder = builder;
            _cache = cache;
        }

        /// <summary>
        /// Preload texts pre všetky jazyky a obtiažnosti
        /// </summary>
        public async Task PreloadAllAsync(int textsPerCombination = 10)
        {
            Console.WriteLine("🚀 Starting text preload...");

            var languages = new[] { "en", "sk", "ar", "ja" };
            var difficulties = new[] 
            { 
                DifficultyLevel.Beginner, 
                DifficultyLevel.Intermediate, 
                DifficultyLevel.Advanced 
            };

            var tasks = new List<Task>();

            foreach (var lang in languages)
            {
                foreach (var difficulty in difficulties)
                {
                    tasks.Add(PreloadAsync(lang, difficulty, textsPerCombination));
                }
            }

            await Task.WhenAll(tasks);

            var stats = _cache.GetStats();
            Console.WriteLine($"✅ Preload complete! Total cached: {stats.TotalCachedTexts} texts");
        }

        /// <summary>
        /// Preload texts pre konkrétny jazyk a obtiažnosť
        /// </summary>
        public async Task PreloadAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count = 10)
        {
            try
            {
                Console.WriteLine($"📦 Preloading {count} {difficulty} texts for {languageCode}...");

                var provider = _builder
                    .ForLanguage(languageCode)
                    .ForDifficulty(difficulty)
                    .Build();

                var texts = await provider.GetTextsAsync(null, count);

                if (texts.Any())
                {
                    _cache.CacheTexts(languageCode, difficulty, texts);
                    Console.WriteLine($"  ✅ Cached {texts.Count} texts ({languageCode} - {difficulty})");
                }
                else
                {
                    Console.WriteLine($"  ⚠️ No texts fetched ({languageCode} - {difficulty})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Preload failed ({languageCode} - {difficulty}): {ex.Message}");
            }
        }
    }
}