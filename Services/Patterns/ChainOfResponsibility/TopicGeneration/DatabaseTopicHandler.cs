using Supabase;
using POT_SEM.Core.Models;
using POT_SEM.Services.Databases;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility.TopicGeneration
{
    /// <summary>
    /// First handler: Try to get topics from database (fastest, most relevant)
    /// </summary>
    public class DatabaseTopicHandler : TopicGenerationHandler
    {
        private readonly Client _supabase;
        
        public DatabaseTopicHandler(Client supabase)
        {
            _supabase = supabase;
        }
        
        public override async Task<List<string>> HandleAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count)
        {
            try
            {
                Console.WriteLine($"DATABASE TOPIC HANDLER: Trying to fetch {count} topics for {languageCode}/{difficulty}");
                
                var difficultyStr = difficulty.ToString();
                
                var response = await _supabase
                    .From<DatabaseText>()
                    .Select("title")
                    .Where(x => x.LanguageCode == languageCode)
                    .Where(x => x.Difficulty == difficultyStr)
                    .Limit(count * 3)
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
                    Console.WriteLine($"DATABASE HIT: Found {topics.Count} topics");
                    return topics;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DATABASE MISS: {ex.Message}");
            }
            
            // Delegate to next handler
            Console.WriteLine("DATABASE MISS: Passing to next handler");
            return _nextHandler != null 
                ? await _nextHandler.HandleAsync(languageCode, difficulty, count)
                : new List<string>();
        }
    }
}
