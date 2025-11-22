using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;

namespace POT_SEM.Core.BridgeAbstractions
{
    public class BeginnerTextProvider : TextProvider
    {
        public BeginnerTextProvider(ILanguageTextSource languageSource) 
            : base(languageSource)
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
                MinWordCount = 50,
                MaxWordCount = 300
            };
        }
        
        protected override List<Text> ApplyDifficultyFilters(List<Text> texts)
        {
            return texts
                .Where(t => 
                {
                    var avgSentenceLength = CalculateAverageSentenceLength(t.Content);
                    return avgSentenceLength < 15;
                })
                .ToList();
        }
        
        protected override List<Text> ProcessTexts(List<Text> texts)
        {
            foreach (var text in texts)
            {
                // Calculate estimated reading time (minutes) but do not assign it to Metadata
                // because TextMetadata does not contain an EstimatedReadingTimeMinutes property.
                text.Metadata.EstimatedReadingTimeMinutes = 
                    (int)Math.Ceiling(text.Metadata.EstimatedWordCount / 100.0);
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
            var sentences = content.Split(new[] { '.', '!', '?' }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            if (sentences.Length == 0) return 0;
            
            var totalWords = sentences.Sum(s => 
                s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            
            return (double)totalWords / sentences.Length;
        }
    }
}