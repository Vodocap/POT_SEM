using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Adapters
{
    /// <summary>
    /// Adapter: IRandomWordService → ITopicGenerationStrategy
    /// </summary>
    public class RandomWordTopicAdapter : ITopicGenerationStrategy
    {
        private readonly IRandomWordService _randomWordService;
        
        public string StrategyName => $"{_randomWordService.ServiceName} (Adapter)";
        
        public RandomWordTopicAdapter(IRandomWordService randomWordService)
        {
            _randomWordService = randomWordService;
        }
        
        public async Task<List<string>> GenerateTopicsAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count)
        {
            // ✅ Deleguje na IRandomWordService
            return await _randomWordService.GetRandomWordsAsync(languageCode, count);
        }
    }
}