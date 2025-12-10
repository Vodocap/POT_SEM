using POT_SEM.Services.Patterns.Strategy;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility.Translation
{
    /// <summary>
    /// Final handler: AI translation API handler
    /// </summary>
    public class ApiTranslationHandler : TranslationHandler
    {
        private readonly ApiTranslationService _api;
        private readonly DatabaseTranslationService? _database;
        
        public ApiTranslationHandler(
            ApiTranslationService api, 
            DatabaseTranslationService? database = null)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _database = database;
        }
        
        public override async Task<string?> HandleAsync(string word, string sourceLang, string targetLang)
        {
            Console.WriteLine($"API REQUEST: {word}");
            
            var result = await _api.TranslateWordAsync(word, sourceLang, targetLang);
            
            if (result != null)
            {
                Console.WriteLine($"API SUCCESS: {word} -> {result}");
                
                if (_database != null)
                {
                    try
                    {
                        await _database.SaveTranslationAsync(word, result, sourceLang, targetLang);
                        Console.WriteLine($"SAVED TO DB: {word}");
                    }
                    catch
                    {
                        Console.WriteLine($" API TRANSLATION HANDLER FAILED TO SAVE TO DB: {word}");
                    }
                }
                
                return result;
            }
            
            Console.WriteLine($"CHAIN EXHAUSTED: No handler could translate '{word}'");
            return null;
        }
    }
}
