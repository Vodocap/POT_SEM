using Supabase;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.TopicStrategies
{
    /// <summary>
    /// 💾 Database-first topic strategy with static fallback
    /// </summary>
    public class DatabaseTopicStrategy : ITopicGenerationStrategy
    {
        private readonly Client _supabase;
        private readonly StaticTopicStrategy _fallback;
        
        public string StrategyName => "Database Topics (with fallback)";
        
        public DatabaseTopicStrategy(Client supabase)
        {
            _supabase = supabase;
            _fallback = new StaticTopicStrategy();
        }
        
        public async Task<List<string>> GenerateTopicsAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count)
        {
            try
            {
                Console.WriteLine($"💾 {StrategyName}: Querying database for {languageCode}/{difficulty}...");
                
                var difficultyStr = difficulty.ToString();
                
                var response = await _supabase
                    .From<Database.DatabaseText>()
                    .Select("title")
                    .Where(x => x.LanguageCode == languageCode)
                    .Where(x => x.Difficulty == difficultyStr)
                    .Limit(count * 3) // Fetch more for variety
                    .Get();
                
                var topics = response.Models
                    .Select(t => t.Title)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(count)
                    .ToList();
                
                if (topics.Any())
                {
                    Console.WriteLine($"   ✅ Found {topics.Count} topics in database");
                    return topics;
                }
                
                Console.WriteLine($"   ⚠️ Database empty for {languageCode}/{difficulty}, using static fallback...");
                return await _fallback.GenerateTopicsAsync(languageCode, difficulty, count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Database error: {ex.Message}, using fallback...");
                return await _fallback.GenerateTopicsAsync(languageCode, difficulty, count);
            }
        }
    }
}