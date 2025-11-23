using POT_SEM.Core.Models;

namespace POT_SEM.Core.Interfaces
{
    /// <summary>
    /// STRATEGY PATTERN - Different ways to generate topics
    /// </summary>
    public interface ITopicGenerationStrategy
    {
        Task<List<string>> GenerateTopicsAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count);
        
        string StrategyName { get; }
    }
}