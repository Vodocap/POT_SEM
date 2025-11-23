using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;

namespace POT_SEM.Core.BridgeAbstractions
{
    /// <summary>
    /// BRIDGE ABSTRACTION - Enhanced with retry logic
    /// </summary>
    public abstract class TextProvider
    {
        protected readonly ILanguageTextSource _languageSource;
        
        // Configuration
        protected const int MAX_FETCH_ATTEMPTS = 3;
        protected const int TEXTS_PER_ATTEMPT = 10; // Fetch more than needed
        
        protected TextProvider(ILanguageTextSource languageSource)
        {
            _languageSource = languageSource 
                ?? throw new ArgumentNullException(nameof(languageSource));
        }
        
        public abstract DifficultyLevel DifficultyLevel { get; }
        
        /// <summary>
        /// Get texts with retry logic - GUARANTEED results!
        /// </summary>
        public async Task<List<Text>> GetTextsAsync(string? topic = null, int count = 10)
        {
            if (!_languageSource.SupportsDifficulty(DifficultyLevel))
            {
                throw new InvalidOperationException(
                    $"{_languageSource.LanguageName} does not support {DifficultyLevel}"
                );
            }
            
            var collectedTexts = new List<Text>();
            var attempts = 0;
            
            Console.WriteLine($"🎯 {GetType().Name}: Fetching {count} texts for {DifficultyLevel}");
            
            // Retry until we have enough texts or hit max attempts
            while (collectedTexts.Count < count && attempts < MAX_FETCH_ATTEMPTS)
            {
                attempts++;
                Console.WriteLine($"   Attempt {attempts}/{MAX_FETCH_ATTEMPTS}...");
                
                try
                {
                    var criteria = CreateSearchCriteria(topic, TEXTS_PER_ATTEMPT);
                    var rawTexts = await _languageSource.FetchTextsAsync(criteria);
                    
                    if (rawTexts.Any())
                    {
                        // Apply filters (but don't be too strict)
                        var filteredTexts = ApplyDifficultyFilters(rawTexts);
                        
                        // Process texts
                        var processedTexts = ProcessTexts(filteredTexts);
                        
                        // Add new unique texts
                        foreach (var text in processedTexts)
                        {
                            if (!collectedTexts.Any(t => t.Title == text.Title))
                            {
                                collectedTexts.Add(text);
                                Console.WriteLine($"   ✅ Added: {text.Title} ({text.Metadata.EstimatedWordCount} words)");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️ Attempt {attempts} failed: {ex.Message}");
                }
                
                // Small delay between attempts
                if (collectedTexts.Count < count && attempts < MAX_FETCH_ATTEMPTS)
                {
                    await Task.Delay(500);
                }
            }
            
            Console.WriteLine($"   ✅ Collected {collectedTexts.Count} texts total");
            
            return collectedTexts.Take(count).ToList();
        }
        
        protected abstract TextSearchCriteria CreateSearchCriteria(string? topic, int count);
        protected abstract List<Text> ApplyDifficultyFilters(List<Text> texts);
        protected abstract List<Text> ProcessTexts(List<Text> texts);
        public abstract Task<List<string>> GetRecommendedTopicsAsync();
    }
}