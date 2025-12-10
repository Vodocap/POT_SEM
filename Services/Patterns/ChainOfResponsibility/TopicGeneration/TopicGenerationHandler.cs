using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility.TopicGeneration
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY - Abstract Handler for topic generation
    /// </summary>
    public abstract class TopicGenerationHandler
    {
        protected TopicGenerationHandler? _nextHandler;
        
        public void SetNext(TopicGenerationHandler handler)
        {
            _nextHandler = handler;
        }
        
        public abstract Task<List<string>> HandleAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count);
    }
}
