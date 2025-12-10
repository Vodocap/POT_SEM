using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using POT_SEM.Services;


namespace POT_SEM.Services.TextProviders
{
    public class IntermediateTextProvider : TextProvider
    {
        public IntermediateTextProvider(
        ILanguageTextSource languageSource,
        ITextCacheService? cache = null) 
        : base(languageSource, cache)
    {
    }
        
        public override DifficultyLevel DifficultyLevel => DifficultyLevel.Intermediate;
        
        protected override TextSearchCriteria CreateSearchCriteria(string? topic, int count)
        {
            return new TextSearchCriteria
            {
                Difficulty = DifficultyLevel.Intermediate,
                Language = _languageSource.LanguageCode,
                Topic = topic,
                MinWordCount = 150,
                MaxWordCount = 2000,
                MaxResults = count
            };
        }
        
        protected override List<Text> ApplyDifficultyFilters(List<Text> texts)
        {
            return texts
                .Where(t => 
                {
                    var wordCount = t.Metadata.EstimatedWordCount;
                    
                    if (wordCount >= 100 && wordCount <= 2500)
                    {
                        return true;
                    }
                    
                    var avgSentenceLength = CalculateAverageSentenceLength(t.Content);
                    return avgSentenceLength >= 5 && avgSentenceLength <= 30;
                })
                .ToList();
        }
        
        protected override List<Text> ProcessTexts(List<Text> texts)
        {
            foreach (var text in texts)
            {
                if (text.Metadata.EstimatedWordCount > 1500)
                {
                    text.Content = TruncateToWords(text.Content, 1500);
                    text.Metadata.EstimatedWordCount = 1500;
                }
                
                text.Metadata.EstimatedReadingTimeMinutes = 
                    Math.Max(1, (int)Math.Ceiling(text.Metadata.EstimatedWordCount / 150.0));
            }
            
            return texts.OrderBy(t => t.Metadata.EstimatedWordCount).ToList();
        }
        
        public override async Task<List<string>> GetRecommendedTopicsAsync()
        {
            return await _languageSource.GetAvailableTopicsAsync();
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
        
        private string TruncateToWords(string content, int maxWords)
        {
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length <= maxWords)
            {
                return content;
            }
            
            var truncated = string.Join(" ", words.Take(maxWords));
            
            var lastSentenceEnd = Math.Max(
                truncated.LastIndexOf('.'),
                Math.Max(truncated.LastIndexOf('!'), truncated.LastIndexOf('?'))
            );
            
            if (lastSentenceEnd > truncated.Length * 0.8)
            {
                return truncated.Substring(0, lastSentenceEnd + 1);
            }
            
            return truncated + "...";
        }
    }
}