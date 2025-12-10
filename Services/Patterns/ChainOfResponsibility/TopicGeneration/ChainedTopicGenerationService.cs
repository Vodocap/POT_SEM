using Supabase;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.Strategy.RandomWord;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility.TopicGeneration
{
    /// <summary>
    /// Builds and manages topic generation chain: Database -> Wikipedia -> Static
    /// Implements ITopicGenerationStrategy for compatibility
    /// </summary>
    public class ChainedTopicGenerationService : ITopicGenerationStrategy
    {
        private readonly TopicGenerationHandler _handlerChain;
        
        public string StrategyName => "Chained (Database → Wikipedia → Static)";
        
        public ChainedTopicGenerationService(
            Client supabase,
            WikipediaRandomWordService wikipediaService)
        {
            var staticHandler = new StaticTopicHandler();
            
            var wikipediaHandler = new WikipediaTopicHandler(wikipediaService);
            wikipediaHandler.SetNext(staticHandler);
            
            var databaseHandler = new DatabaseTopicHandler(supabase);
            databaseHandler.SetNext(wikipediaHandler);
            
            _handlerChain = databaseHandler;
            
            Console.WriteLine("TOPIC CHAIN BUILT: Database → Wikipedia → Static");
        }
        
        public async Task<List<string>> GenerateTopicsAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count)
        {
            Console.WriteLine($"TOPIC CHAIN START: '{languageCode}/{difficulty}' (count: {count})");
            var result = await _handlerChain.HandleAsync(languageCode, difficulty, count);
            Console.WriteLine($"TOPIC CHAIN END: Generated {result.Count} topics");
            return result;
        }
    }
}
