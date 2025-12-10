using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using POT_SEM.Services;

namespace POT_SEM.Services.TextProviders
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
                MinWordCount = 30, 
                MaxWordCount = 400,
                MaxResults = count
            };
        }
        
        protected override List<Text> ApplyDifficultyFilters(List<Text> texts)
        {
            return texts
                .Where(t => 
                {
                    var wordCount = t.Metadata.EstimatedWordCount;
                    
                    if (wordCount >= 30 && wordCount <= 500)
                    {
                        return true;
                    }
                    
                    var avgSentenceLength = CalculateAverageSentenceLength(t.Content);
                    return avgSentenceLength > 0 && avgSentenceLength < 20;
                })
                .ToList();
        }
        
        protected override List<Text> ProcessTexts(List<Text> texts)
        {
            foreach (var text in texts)
            {
                if (text.Metadata.EstimatedWordCount > 300)
                {
                    text.Content = TruncateToSentences(text.Content, 3);
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
            
            if (!truncated.EndsWith(".") && !truncated.EndsWith("。"))
            {
                truncated += ".";
            }
            
            return truncated;
        }
    }
}