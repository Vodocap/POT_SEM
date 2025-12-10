using POT_SEM.Core.Interfaces;
using POT_SEM.Services.Patterns.Strategy;
using POT_SEM.Services.Patterns.Flyweight;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility.Translation
{
    /// <summary>
    /// Builds and manages the handler chain: Flyweight → Dictionary → API
    /// Acts as a unified interface (Strategy pattern) while using Chain of Responsibility internally
    /// </summary>
    public class ChainedTranslationService : ITranslationStrategy
    {
        private readonly TranslationHandler _handlerChain;
        private readonly ApiTranslationService _api;
        private readonly DatabaseTranslationService? _database;
        private readonly WordFlyweightFactory _flyweightFactory;
        
        public string StrategyName => "Chained (Flyweight → Dictionary → API)";
        
        public ChainedTranslationService(
            WordFlyweightFactory flyweightFactory,
            DictionaryTranslationStrategy? dictionary,
            DatabaseTranslationService? database,
            ApiTranslationService api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _flyweightFactory = flyweightFactory ?? throw new ArgumentNullException(nameof(flyweightFactory));
            _database = database;
            
            var apiHandler = new ApiTranslationHandler(api, database);
            
            TranslationHandler chain = apiHandler;
            if (dictionary != null)
            {
                var dictHandler = new DictionaryTranslationHandler(dictionary);
                dictHandler.SetNext(apiHandler);
                chain = dictHandler;
            }
            
            var flyweightHandler = new FlyweightTranslationHandler(flyweightFactory);
            flyweightHandler.SetNext(chain);
            
            _handlerChain = flyweightHandler;
            
            Console.WriteLine($"CHAIN BUILT: Flyweight (memory+DB) -> {(dictionary != null ? "Dictionary -> " : "")}API");
        }

        public async Task SaveTranslationToDatabaseAsync(string originalWord, string translation, string sourceLang, string targetLang, string? transliteration = null, string? furigana = null)
        {
            if (_database == null) return;
            try
            {
                await _database.SaveTranslationAsync(originalWord, translation, sourceLang, targetLang, transliteration, furigana);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Failed to save to database: {ex.Message}");
            }
        }
        
        public async Task<string?> TranslateWordAsync(string word, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return null;
            }
            
            Console.WriteLine($"CHAIN START: '{word}' ({sourceLang} -> {targetLang})");
            var result = await _handlerChain.HandleAsync(word, sourceLang, targetLang);
            Console.WriteLine(result != null ? $"SUCCESS: '{word}' -> '{result}'" : $"FAILED: '{word}'");
            
            return result;
        }
        
        public async Task<string?> TranslateSentenceAsync(string sentence, string sourceLang, string targetLang)
        {
            return await _api.TranslateSentenceAsync(sentence, sourceLang, targetLang);
        }
        
        public async Task<Dictionary<string, string>> TranslateBatchAsync(
            IEnumerable<string> words, 
            string sourceLang, 
            string targetLang)
        {
            var results = new Dictionary<string, string>();
            var wordsToTranslate = new List<string>(words);
            
            foreach (var word in wordsToTranslate.ToList())
            {
                var cached = _flyweightFactory.GetTranslation(sourceLang, targetLang, word);
                if (cached != null)
                {
                    results[word] = cached;
                    wordsToTranslate.Remove(word);
                }
            }
            
            if (!wordsToTranslate.Any())
            {
                Console.WriteLine($"BATCH: All {results.Count} words cached");
                return results;
            }
            
            Console.WriteLine($"BATCH: {wordsToTranslate.Count} need translation ({results.Count} cached)");
            
            foreach (var word in wordsToTranslate)
            {
                var translation = await _handlerChain.HandleAsync(word, sourceLang, targetLang);
                if (translation != null)
                {
                    results[word] = translation;
                }
            }
            
            Console.WriteLine($"BATCH COMPLETE: {results.Count}/{words.Count()} translated");
            return results;
        }
    }
}
