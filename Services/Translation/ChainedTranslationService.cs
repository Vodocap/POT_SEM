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
        private readonly DatabaseTranslationService? _database;  // ← Nullable
        private readonly ApiTranslationService _api;
        
        public string StrategyName => _database != null 
            ? "Chained (Cache → DB → API)" 
            : "Chained (Cache → API)";
        
        public ChainedTranslationService(
            TranslationFlyweightFactory flyweight,
            DatabaseTranslationService? database,  // ← Nullable
            ApiTranslationService api)
        {
            _flyweight = flyweight;
            _database = database;
            _api = api;
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
            
            // STEP 2: Try DATABASE (if available)
            if (_database != null)
            {
                var dbResult = await _database.TranslateWordAsync(word, sourceLang, targetLang);
                if (dbResult != null)
                {
                    Console.WriteLine($"   ✅ DATABASE HIT");
                    _flyweight.AddTranslation(sourceLang, targetLang, word, dbResult);
                    return dbResult;
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
            
            // STEP 1: Check flyweight
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
            
            // STEP 2: Check database (if available)
            if (_database != null)
            {
                var dbResults = await _database.TranslateBatchAsync(wordsToTranslate, sourceLang, targetLang);
                foreach (var (word, translation) in dbResults)
                {
                    results[word] = translation;
                    _flyweight.AddTranslation(sourceLang, targetLang, word, translation);
                    wordsToTranslate.Remove(word);
                }
                
                Console.WriteLine($"   💾 DATABASE: {dbResults.Count} found, {wordsToTranslate.Count} remaining");
            }
            
            if (!wordsToTranslate.Any())
            {
                return results;
            }
            
            // STEP 3: Use API
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