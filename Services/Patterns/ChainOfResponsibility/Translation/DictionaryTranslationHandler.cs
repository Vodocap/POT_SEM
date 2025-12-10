using POT_SEM.Services.Patterns.Strategy;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility.Translation
{
    /// <summary>
    /// Dictionary API handler providing rich word meanings (AI-based, slower than DB).
    /// </summary>
    public class DictionaryTranslationHandler : TranslationHandler
    {
        private readonly DictionaryTranslationStrategy _dictionary;
        
        public DictionaryTranslationHandler(DictionaryTranslationStrategy dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }
        
        public override async Task<string?> HandleAsync(string word, string sourceLang, string targetLang)
        {
            // Try dictionary API (AI-based, slower)
            var result = await _dictionary.TranslateWordAsync(word, sourceLang, targetLang);
            
            if (result != null)
            {
                Console.WriteLine($"DICTIONARY HIT: {word} -> {result}");
                return result;
            }
            
            Console.WriteLine($"DICTIONARY MISS: {word} - passing to next handler");
            
            // Not found in dictionary, delegate to next handler
            return _nextHandler != null 
                ? await _nextHandler.HandleAsync(word, sourceLang, targetLang) 
                : null;
        }
    }
}
