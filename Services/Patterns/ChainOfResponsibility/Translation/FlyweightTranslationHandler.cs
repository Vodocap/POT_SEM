using POT_SEM.Services.Patterns.Flyweight;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility.Translation
{
    /// <summary>
    /// Flyweight handler with database integration. Auto-loads from DB and caches in memory.
    /// </summary>
    public class FlyweightTranslationHandler : TranslationHandler
    {
        private readonly WordFlyweightFactory _flyweightFactory;
        
        public FlyweightTranslationHandler(WordFlyweightFactory flyweightFactory)
        {
            _flyweightFactory = flyweightFactory ?? throw new ArgumentNullException(nameof(flyweightFactory));
        }
        
        public override async Task<string?> HandleAsync(string word, string sourceLang, string targetLang)
        {
            // Get or create flyweight (auto-loads from database if new)
            var flyweight = await _flyweightFactory.GetOrCreateAsync(word, sourceLang, targetLang);
            
            if (flyweight.Translation != null)
            {
                Console.WriteLine($"FLYWEIGHT HIT (memory or DB): {word}");
                return flyweight.Translation;
            }
            
            Console.WriteLine($"FLYWEIGHT MISS: {word} - passing to next handler");
            
            // Not in flyweight pool or database, delegate to next handler
            if (_nextHandler != null)
            {
                var result = await _nextHandler.HandleAsync(word, sourceLang, targetLang);
                
                // Save result to flyweight and database
                if (result != null)
                {
                    try
                    {
                        await _flyweightFactory.UpdateTranslationAsync(flyweight, result);
                        Console.WriteLine($"FLYWEIGHT SAVED (memory + DB): {word} -> {result}");
                    }
                    catch
                    {
                    }
                }
                
                return result;
            }
            
            return null;
        }
    }
}
