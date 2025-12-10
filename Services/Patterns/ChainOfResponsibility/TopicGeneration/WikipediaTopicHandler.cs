using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.Strategy.RandomWord;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility.TopicGeneration
{
    /// <summary>
    /// Second handler: Try Wikipedia Random Page API
    /// </summary>
    public class WikipediaTopicHandler : TopicGenerationHandler
    {
        private readonly WikipediaRandomWordService _wikipediaService;
        
        public WikipediaTopicHandler(WikipediaRandomWordService wikipediaService)
        {
            _wikipediaService = wikipediaService;
        }
        
        public override async Task<List<string>> HandleAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count)
        {
            try
            {
                Console.WriteLine($"WIKIPEDIA TOPIC HANDLER: Trying to fetch {count} topics for {languageCode}");
                
                if (await _wikipediaService.IsAvailableAsync())
                {
                    var topics = await _wikipediaService.GetRandomWordsAsync(languageCode, count);
                    
                    if (topics.Any())
                    {
                        Console.WriteLine($"WIKIPEDIA HIT: Found {topics.Count} topics");
                        return topics;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WIKIPEDIA MISS: {ex.Message}");
            }
            
            // Delegate to next handler
            Console.WriteLine("WIKIPEDIA MISS: Passing to next handler");
            return _nextHandler != null 
                ? await _nextHandler.HandleAsync(languageCode, difficulty, count)
                : new List<string>();
        }
    }
}
