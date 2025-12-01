using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Patterns.Bridge
{
    /// <summary>
    /// BRIDGE ABSTRACTION - Enhanced with caching
    /// </summary>
    public abstract class TextProvider
    {
        protected readonly ILanguageTextSource _languageSource;
        protected readonly ITextCacheService? _cache; // ✅ Changed to protected
        
        protected const int MAX_FETCH_ATTEMPTS = 3;
        protected const int TEXTS_PER_ATTEMPT = 10;
        
        protected TextProvider(
            ILanguageTextSource languageSource,
            ITextCacheService? cache = null)
        {
            _languageSource = languageSource 
                ?? throw new ArgumentNullException(nameof(languageSource));
            _cache = cache;
        }
        
        public abstract DifficultyLevel DifficultyLevel { get; }
        
        /// <summary>
        /// Get texts - use cache if available!
        /// </summary>
        public async Task<List<Text>> GetTextsAsync(string? topic = null, int count = 10)
        {
            // 1. Try cache first (only for non-topic requests)
            if (_cache != null && string.IsNullOrEmpty(topic))
            {
                var cached = _cache.GetCachedTexts(_languageSource.LanguageCode, DifficultyLevel);
                if (cached != null && cached.Any())
                {
                    return cached.Take(count).ToList();
                }
            }
            
            // 2. Cache miss - fetch fresh
            
            if (!_languageSource.SupportsDifficulty(DifficultyLevel))
            {
                throw new InvalidOperationException(
                    $"{_languageSource.LanguageName} does not support {DifficultyLevel}"
                );
            }
            
            var collectedTexts = new List<Text>();
            var attempts = 0;
            
            while (collectedTexts.Count < count && attempts < MAX_FETCH_ATTEMPTS)
            {
                attempts++;
                
                try
                {
                    var criteria = CreateSearchCriteria(topic, TEXTS_PER_ATTEMPT);
                    var rawTexts = await _languageSource.FetchTextsAsync(criteria);
                    
                    if (rawTexts.Any())
                    {
                        var filteredTexts = ApplyDifficultyFilters(rawTexts);
                        var processedTexts = ProcessTexts(filteredTexts);
                        
                        foreach (var text in processedTexts)
                        {
                            if (!collectedTexts.Any(t => t.Title == text.Title))
                            {
                                collectedTexts.Add(text);
                            }
                        }
                    }
                }
                catch
                {
                    // Attempt failed, continue to next attempt
                }
                
                if (collectedTexts.Count < count && attempts < MAX_FETCH_ATTEMPTS)
                {
                    await Task.Delay(500);
                }
            }
            
            // 3. Cache the results (if we fetched without topic)
            if (_cache != null && string.IsNullOrEmpty(topic) && collectedTexts.Any())
            {
                _cache.CacheTexts(_languageSource.LanguageCode, DifficultyLevel, collectedTexts);
            }
            
            return collectedTexts.Take(count).ToList();
        }
        
        protected abstract TextSearchCriteria CreateSearchCriteria(string? topic, int count);
        protected abstract List<Text> ApplyDifficultyFilters(List<Text> texts);
        protected abstract List<Text> ProcessTexts(List<Text> texts);
        public abstract Task<List<string>> GetRecommendedTopicsAsync();
    }
}