using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Translation
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY PATTERN
    /// Tries multiple sources in order:
    /// 1. FLYWEIGHT (in-memory) - fastest
    /// 2. DATABASE (Supabase) - fast (if available)
    /// 3. API (external) - slowest but always works
    /// </summary>
    public class ChainedTranslationService : ITranslationStrategy
    {
        private readonly TranslationFlyweightFactory _flyweight;
        private readonly DictionaryTranslationStrategy? _dictionary;  // ← Dictionary strategy
        private readonly DatabaseTranslationService? _database;  // ← Nullable
        private readonly ApiTranslationService _api;
        
        public string StrategyName => _dictionary != null
            ? "Chained (Cache → Dictionary → DB → API)"
            : _database != null 
                ? "Chained (Cache → DB → API)" 
                : "Chained (Cache → API)";
        
        public ChainedTranslationService(
            TranslationFlyweightFactory flyweight,
            DictionaryTranslationStrategy? dictionary,  // ← Dictionary strategy parameter
            DatabaseTranslationService? database,  // ← Nullable
            ApiTranslationService api)
        {
            _flyweight = flyweight;
            _dictionary = dictionary;
            _database = database;
            _api = api;
        }

        /// <summary>
        /// Persist a translation into the configured database if available.
        /// This allows callers (e.g., dictionary assigners) to store translations discovered from other sources.
        /// </summary>
        public async Task SaveTranslationToDatabaseAsync(string originalWord, string translation, string sourceLang, string targetLang, string? transliteration = null, string? furigana = null)
        {
            if (_database == null) return;
            try
            {
                await _database.SaveTranslationAsync(originalWord, translation, sourceLang, targetLang, transliteration, furigana);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to save dictionary-derived translation to DB for '{originalWord}': {ex.Message}");
            }
        }
        
        public async Task<string?> TranslateWordAsync(string word, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return null;
            }
            
            Console.WriteLine($"🔗 CHAIN translating: '{word}'");
            
            // STEP 1: Try FLYWEIGHT (in-memory cache)
            var cached = _flyweight.GetTranslation(sourceLang, targetLang, word);
            if (cached != null)
            {
                Console.WriteLine($"   ✅ FLYWEIGHT HIT");
                return cached;
            }

            // STEP 2: Try DICTIONARY strategy (if available)
            if (_dictionary != null)
            {
                var dictResult = await _dictionary.TranslateWordAsync(word, sourceLang, targetLang);
                if (dictResult != null)
                {
                    // Cache the dictionary result
                    try { _flyweight.AddTranslation(sourceLang, targetLang, word, dictResult); } catch { }
                    
                    // Save to database if available
                    if (_database != null)
                    {
                        _ = Task.Run(async () => await SaveTranslationToDatabaseAsync(word, dictResult, sourceLang, targetLang, null, null));
                    }
                    
                    return dictResult;
                }
            }
            
            // STEP 3: Try API (last resort)
            var apiResult = await _api.TranslateWordAsync(word, sourceLang, targetLang);
            if (apiResult != null)
            {
                Console.WriteLine($"   ✅ API SUCCESS");
                _flyweight.AddTranslation(sourceLang, targetLang, word, apiResult);
                
                // Save to database if available
                if (_database != null)
                {
                    await _database.SaveTranslationAsync(word, apiResult, sourceLang, targetLang);
                }
                
                return apiResult;
            }
            
            Console.WriteLine($"   ❌ CHAIN FAILED");
            return null;
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
            
            Console.WriteLine($"🔗 CHAIN batch: {wordsToTranslate.Count} words");
            
            // STEP 1: Check flyweight cache
            foreach (var word in wordsToTranslate.ToList())
            {
                var cached = _flyweight.GetTranslation(sourceLang, targetLang, word);
                if (cached != null)
                {
                    results[word] = cached;
                    wordsToTranslate.Remove(word);
                }
            }
            
            Console.WriteLine($"   🪶 FLYWEIGHT: {results.Count} cached, {wordsToTranslate.Count} remaining");
            
            if (!wordsToTranslate.Any())
            {
                return results;
            }
            
            // STEP 2: Try dictionary strategy for remaining words
            if (_dictionary != null)
            {
                var dictResults = await _dictionary.TranslateBatchAsync(wordsToTranslate, sourceLang, targetLang);
                foreach (var (word, translation) in dictResults)
                {
                    results[word] = translation;
                    try { _flyweight.AddTranslation(sourceLang, targetLang, word, translation); } catch { }
                    wordsToTranslate.Remove(word);
                    
                    // Save to database if available
                    if (_database != null)
                    {
                        _ = Task.Run(async () => await _database.SaveTranslationAsync(word, translation, sourceLang, targetLang));
                    }
                }
            }
            
            if (!wordsToTranslate.Any())
            {
                return results;
            }
            
            // STEP 3: Use API for remaining words
            var apiResults = await _api.TranslateBatchAsync(wordsToTranslate, sourceLang, targetLang);
            foreach (var (word, translation) in apiResults)
            {
                results[word] = translation;
                _flyweight.AddTranslation(sourceLang, targetLang, word, translation);
                
                // Save to database if available
                if (_database != null)
                {
                    await _database.SaveTranslationAsync(word, translation, sourceLang, targetLang);
                }
            }
            
            Console.WriteLine($"   🌐 API: {apiResults.Count} translated");
            
            return results;
        }
    }
}