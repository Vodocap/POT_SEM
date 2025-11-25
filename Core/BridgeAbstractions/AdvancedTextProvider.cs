using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using POT_SEM.Services;

namespace POT_SEM.Core.BridgeAbstractions
{
    public class AdvancedTextProvider : TextProvider
    {
        public AdvancedTextProvider(
        ILanguageTextSource languageSource,
        ITextCacheService? cache = null) 
        : base(languageSource, cache)
    {
    }
        
        public override DifficultyLevel DifficultyLevel => DifficultyLevel.Advanced;
        
        protected override TextSearchCriteria CreateSearchCriteria(string? topic, int count)
        {
            return new TextSearchCriteria
            {
                Difficulty = DifficultyLevel.Advanced,
                Language = _languageSource.LanguageCode,
                Topic = topic,
                MinWordCount = 400,  // LOWERED - was 1000 (Wikipedia extracts rarely exceed 500)
                MaxWordCount = 10000,
                MaxResults = count
            };
        }
        
        protected override List<Text> ApplyDifficultyFilters(List<Text> texts)
        {
            // REALISTIC - Wikipedia extracts are typically 200-800 words
            // Accept anything with reasonable content
            return texts
                .Where(t => 
                {
                    var wordCount = t.Metadata.EstimatedWordCount;
                    
                    // Accept texts that are substantial (lowered threshold)
                    return wordCount >= 300; // Much lower than 1000
                })
                .ToList();
        }
        
        protected override List<Text> ProcessTexts(List<Text> texts)
        {
            foreach (var text in texts)
            {
                // For advanced, we want the full text
                // Calculate reading time
                text.Metadata.EstimatedReadingTimeMinutes = 
                    Math.Max(2, (int)Math.Ceiling(text.Metadata.EstimatedWordCount / 200.0));
            }
            
            // Order by word count (longest first for advanced)
            return texts.OrderByDescending(t => t.Metadata.EstimatedWordCount).ToList();
        }
        
        public override async Task<List<string>> GetRecommendedTopicsAsync()
        {
            return await _languageSource.GetAvailableTopicsAsync();
        }
    }
}