using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.RandomWordServices;

namespace POT_SEM.Services.TopicStrategies
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
            Console.WriteLine($"🎲 {StrategyName}: Generating {count} topics for {languageCode}");
            
            try
            {
                // Try primary service (Wikipedia)
                if (await _primaryService.IsAvailableAsync())
                {
                    Console.WriteLine($"   Using {_primaryService.ServiceName}");
                    var topics = await _primaryService.GetRandomWordsAsync(languageCode, count);
                    
                    if (topics.Any())
                    {
                        return topics;
                    }
                }
                
                Console.WriteLine($"   ⚠️ Primary service unavailable, using fallback");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Primary service error: {ex.Message}");
            }
            
            // Fallback to minimal pool
            Console.WriteLine($"   Using {_fallbackService.ServiceName}");
            return await _fallbackService.GetRandomWordsAsync(languageCode, count);
        }
    }
}