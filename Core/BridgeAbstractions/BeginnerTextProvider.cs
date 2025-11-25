using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using POT_SEM.Services;

namespace POT_SEM.Core.BridgeAbstractions
{
    public class BeginnerTextProvider : TextProvider
    {
        public BeginnerTextProvider(
        ILanguageTextSource languageSource,
        ITextCacheService? cache = null) 
        : base(languageSource, cache)
    {
    }
        
        public override DifficultyLevel DifficultyLevel => DifficultyLevel.Beginner;
        
        protected override TextSearchCriteria CreateSearchCriteria(string? topic, int count)
        {
            return new TextSearchCriteria
            {
                Difficulty = DifficultyLevel.Beginner,
                Language = _languageSource.LanguageCode,
                Topic = topic,
                MinWordCount = 30,  // LOWERED - was 50
                MaxWordCount = 400,
                MaxResults = count
            };
        }
        
        protected override List<Text> ApplyDifficultyFilters(List<Text> texts)
        {
            // LESS STRICT - accept most texts, just exclude very long ones
            return texts
                .Where(t => 
                {
                    var wordCount = t.Metadata.EstimatedWordCount;
                    
                    // Accept if within reasonable range
                    if (wordCount >= 30 && wordCount <= 500)
                    {
                        return true;
                    }
                    
                    // Also check sentence complexity (but don't be too strict)
                    var avgSentenceLength = CalculateAverageSentenceLength(t.Content);
                    return avgSentenceLength > 0 && avgSentenceLength < 20; // RELAXED from 15
                })
                .ToList();
        }
        
        protected override List<Text> ProcessTexts(List<Text> texts)
        {
            foreach (var text in texts)
            {
                // Truncate to beginner length if needed
                if (text.Metadata.EstimatedWordCount > 300)
                {
                    text.Content = TruncateToSentences(text.Content, 3); // Keep first 3 sentences
                    text.Metadata.EstimatedWordCount = 
                        text.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                }
                
                text.Metadata.EstimatedReadingTimeMinutes = 
                    Math.Max(1, (int)Math.Ceiling(text.Metadata.EstimatedWordCount / 100.0));
            }
            
            return texts.OrderBy(t => t.Metadata.EstimatedWordCount).ToList();
        }
        
        public override async Task<List<string>> GetRecommendedTopicsAsync()
        {
            var allTopics = await _languageSource.GetAvailableTopicsAsync();
            return allTopics.Take(5).ToList();
        }
        
        private double CalculateAverageSentenceLength(string content)
        {
            var sentences = content.Split(new[] { '.', '!', '?', '。' }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            if (sentences.Length == 0) return 0;
            
            var totalWords = sentences.Sum(s => 
                s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            
            return (double)totalWords / sentences.Length;
        }
        
        private string TruncateToSentences(string content, int sentenceCount)
        {
            var sentences = content.Split(new[] { '.', '!', '?', '。' }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            var truncated = string.Join(". ", sentences.Take(sentenceCount).Select(s => s.Trim()));
            
            // Add period if doesn't end with punctuation
            if (!truncated.EndsWith(".") && !truncated.EndsWith("。"))
            {
                truncated += ".";
            }
            
            return truncated;
        }
    }
}