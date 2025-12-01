using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.Strategy.RandomWord;

namespace POT_SEM.Services.Patterns.Strategy.Topic
{
    /// <summary>
    /// STRATEGY - Uses API services to get random words dynamically
    /// Supports adding ANY language without manual work!
    /// </summary>
    public class ApiTopicStrategy : ITopicGenerationStrategy
    {
        private readonly IRandomWordService _primaryService;
        private readonly IRandomWordService _fallbackService;
        
        public ApiTopicStrategy(
            WikipediaRandomWordService primaryService,
            FallbackWordService fallbackService)
        {
            _primaryService = primaryService;
            _fallbackService = fallbackService;
        }
        
        public string StrategyName => "API Dynamic Strategy";
        
        public async Task<List<string>> GenerateTopicsAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count)
        {
            try
            {
                // Try primary service (Wikipedia)
                if (await _primaryService.IsAvailableAsync())
                {
                    var topics = await _primaryService.GetRandomWordsAsync(languageCode, count);
                    
                    if (topics.Any())
                    {
                        return topics;
                    }
                }
            }
            catch
            {
                // Primary service error
            }
            
            // Fallback to minimal pool
            return await _fallbackService.GetRandomWordsAsync(languageCode, count);
        }
    }
}