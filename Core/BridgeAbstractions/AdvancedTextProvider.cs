using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;

namespace POT_SEM.Core.BridgeAbstractions
{
    public class AdvancedTextProvider : TextProvider
    {
        public AdvancedTextProvider(ILanguageTextSource languageSource) 
            : base(languageSource)
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
                MinWordCount = 1000,
                MaxWordCount = 5000
            };
        }
        
        protected override List<Text> ApplyDifficultyFilters(List<Text> texts)
        {
            return texts.Where(t => t.Metadata.EstimatedWordCount >= 1000).ToList();
        }
        
        protected override List<Text> ProcessTexts(List<Text> texts)
        {
            foreach (var text in texts)
            {
                text.Metadata.EstimatedReadingTimeMinutes = 
                    (int)Math.Ceiling(text.Metadata.EstimatedWordCount / 200.0);
            }
            
            return texts.OrderByDescending(t => t.Metadata.EstimatedWordCount).ToList();
        }
        
        public override async Task<List<string>> GetRecommendedTopicsAsync()
        {
            return await _languageSource.GetAvailableTopicsAsync();
        }
    }
}