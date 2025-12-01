using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using POT_SEM.Services.Patterns.Factory;

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
                var provider = _builder
                    .ForLanguage(languageCode)
                    .ForDifficulty(difficulty)
                    .Build();

                var texts = await provider.GetTextsAsync(null, count);

                if (texts.Any())
                {
                    _cache.CacheTexts(languageCode, difficulty, texts);
                }
            }
            catch (Exception)
            {
                // Preload failed, continue
            }
        }
    }
}