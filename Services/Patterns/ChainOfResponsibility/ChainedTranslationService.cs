using POT_SEM.Core.Interfaces;
using POT_SEM.Services.Patterns.Strategy;
using POT_SEM.Services.Patterns.Flyweight;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY PATTERN - Abstract Handler
    /// Base class for translation handlers in the chain
    /// </summary>
    public abstract class TranslationHandler
    {
        protected TranslationHandler? _nextHandler;
        
        public TranslationHandler SetNext(TranslationHandler handler)
        {
            _nextHandler = handler;
            return handler;
        }
        
        public abstract Task<string?> HandleAsync(string word, string sourceLang, string targetLang);
    }
    
    /// <summary>
    /// Concrete Handler - Cache lookup (fast memory cache)
    /// </summary>
    public class CacheTranslationHandler : TranslationHandler
    {
        private readonly TranslationCacheService _cache;
        
        public CacheTranslationHandler(TranslationCacheService cache)
        {
            _cache = cache;
        }
        
        public override async Task<string?> HandleAsync(string word, string sourceLang, string targetLang)
        {
            var cached = _cache.GetTranslation(sourceLang, targetLang, word);
            if (cached != null)
            {
                return cached;
            }
            
            // Not in cache, try next handler
            if (_nextHandler != null)
            {
                var result = await _nextHandler.HandleAsync(word, sourceLang, targetLang);
                if (result != null)
                {
                    // Cache the result for next time
                    try
                    {
                        _cache.AddTranslation(sourceLang, targetLang, word, result);
                    }
                    catch
                    {
                        // Failed to cache, continue
                    }
                }
                return result;
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// Concrete Handler - Dictionary lookup
    /// </summary>
    public class DictionaryTranslationHandler : TranslationHandler
    {
        private readonly DictionaryTranslationStrategy _strategy;
        
        public DictionaryTranslationHandler(DictionaryTranslationStrategy strategy)
        {
            _strategy = strategy;
        }
        
        public override async Task<string?> HandleAsync(string word, string sourceLang, string targetLang)
        {
            var result = await _strategy.TranslateWordAsync(word, sourceLang, targetLang);
            if (result != null)
            {
                return result;
            }
            
            // Not found, try next handler
            return _nextHandler != null 
                ? await _nextHandler.HandleAsync(word, sourceLang, targetLang) 
                : null;
        }
    }
    
    /// <summary>
    /// Concrete Handler - Database lookup
    /// </summary>
    public class DatabaseTranslationHandler : TranslationHandler
    {
        private readonly DatabaseTranslationService _strategy;
        
        public DatabaseTranslationHandler(DatabaseTranslationService strategy)
        {
            _strategy = strategy;
        }
        
        public override async Task<string?> HandleAsync(string word, string sourceLang, string targetLang)
        {
            var result = await _strategy.TranslateWordAsync(word, sourceLang, targetLang);
            if (result != null)
            {
                return result;
            }
            
            // Not found, try next handler
            return _nextHandler != null 
                ? await _nextHandler.HandleAsync(word, sourceLang, targetLang) 
                : null;
        }
    }
    
    /// <summary>
    /// Concrete Handler - API translation (fallback)
    /// </summary>
    public class ApiTranslationHandler : TranslationHandler
    {
        private readonly ApiTranslationService _strategy;
        private readonly DatabaseTranslationService? _database;
        
        public ApiTranslationHandler(ApiTranslationService strategy, DatabaseTranslationService? database = null)
        {
            _strategy = strategy;
            _database = database;
        }
        
        public override async Task<string?> HandleAsync(string word, string sourceLang, string targetLang)
        {
            // API is the last resort
            var result = await _strategy.TranslateWordAsync(word, sourceLang, targetLang);
            
            // Save API result to database if available
            if (result != null && _database != null)
            {
                try
                {
                    await _database.SaveTranslationAsync(word, result, sourceLang, targetLang);
                }
                catch
                {
                    // Failed to save, continue
                }
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// CHAIN OF RESPONSIBILITY PATTERN - Client
    /// Facade that uses the handler chain
    /// Builds: Cache → Database → Dictionary → API
    /// </summary>
    public class ChainedTranslationService : ITranslationStrategy
    {
        private readonly TranslationHandler _handlerChain;
        private readonly ApiTranslationService _api;
        private readonly DatabaseTranslationService? _database;
        private readonly TranslationCacheService _cache;
        
        public string StrategyName => "Chained (Cache → DB → Dictionary → API)";
        
        public ChainedTranslationService(
            TranslationCacheService cache,
            DictionaryTranslationStrategy? dictionary,
            DatabaseTranslationService? database,
            ApiTranslationService api)
        {
            _api = api;
            _database = database;
            _cache = cache;
            
            // Build the chain from end to start: API (last) - pass database to API handler for saving
            var apiHandler = new ApiTranslationHandler(api, database);
            
            // Add dictionary before API (slow AI API dictionary)
            TranslationHandler chain = apiHandler;
            if (dictionary != null)
            {
                var dictHandler = new DictionaryTranslationHandler(dictionary);
                dictHandler.SetNext(apiHandler);
                chain = dictHandler;
            }
            
            // Add database before dictionary (FAST DB lookup should come before slow AI)
            if (database != null)
            {
                var dbHandler = new DatabaseTranslationHandler(database);
                dbHandler.SetNext(chain);
                chain = dbHandler;
            }
            
            // Add cache at the front (always present, fastest)
            var cacheHandler = new CacheTranslationHandler(cache);
            cacheHandler.SetNext(chain);
            
            _handlerChain = cacheHandler;
        }

        public async Task SaveTranslationToDatabaseAsync(string originalWord, string translation, string sourceLang, string targetLang, string? transliteration = null, string? furigana = null)
        {
            if (_database == null) return;
            try
            {
                await _database.SaveTranslationAsync(originalWord, translation, sourceLang, targetLang, transliteration, furigana);
            }
            catch (Exception)
            {
                // Failed to save to database, continue
            }
        }
        
        public async Task<string?> TranslateWordAsync(string word, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return null;
            }
            
            // Start the chain
            return await _handlerChain.HandleAsync(word, sourceLang, targetLang);
        }
        
        public async Task<string?> TranslateSentenceAsync(string sentence, string sourceLang, string targetLang)
        {
            // Sentences: go directly to API (context matters)
            return await _api.TranslateSentenceAsync(sentence, sourceLang, targetLang);
        }
        
        public async Task<Dictionary<string, string>> TranslateBatchAsync(
            IEnumerable<string> words, 
            string sourceLang, 
            string targetLang)
        {
            var results = new Dictionary<string, string>();
            var wordsToTranslate = new List<string>(words);
            
            // Check cache first
            foreach (var word in wordsToTranslate.ToList())
            {
                var cached = _cache.GetTranslation(sourceLang, targetLang, word);
                if (cached != null)
                {
                    results[word] = cached;
                    wordsToTranslate.Remove(word);
                }
            }
            
            if (!wordsToTranslate.Any())
            {
                return results;
            }
            
            // Translate remaining words through the chain
            foreach (var word in wordsToTranslate)
            {
                var translation = await _handlerChain.HandleAsync(word, sourceLang, targetLang);
                if (translation != null)
                {
                    results[word] = translation;
                }
            }
            
            return results;
        }
    }
}
