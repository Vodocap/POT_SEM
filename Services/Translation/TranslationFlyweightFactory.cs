using System.Collections.Concurrent;

namespace POT_SEM.Services.Translation
{
    /// <summary>
    /// FLYWEIGHT PATTERN - Shared translation cache
    /// Reuses translations for identical words to save memory & API calls
    /// Thread-safe using ConcurrentDictionary
    /// </summary>
    public class TranslationFlyweightFactory
    {
        // Key: "sourceLang:targetLang:word" → Value: translation
        private readonly ConcurrentDictionary<string, string> _translationCache = new();
        
        // Key: "sourceLang:word" → Value: transliteration
        private readonly ConcurrentDictionary<string, string> _transliterationCache = new();
        
        // Key: "ja:word" → Value: furigana (Japanese only)
        private readonly ConcurrentDictionary<string, string> _furiganaCache = new();
        // Key: "lang:word" -> Value: DictionaryEntry (flyweight for dictionary meanings)
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, POT_SEM.Core.Models.DictionaryEntry> _dictionaryCache = new();
        private readonly POT_SEM.Services.Dictionary.WiktionaryService? _wiktionaryService;

        public TranslationFlyweightFactory() { }

        public TranslationFlyweightFactory(POT_SEM.Services.Dictionary.WiktionaryService wiktionaryService)
        {
            _wiktionaryService = wiktionaryService;
        }
        
        /// <summary>
        /// Get cached translation or return null
        /// </summary>
        public string? GetTranslation(string sourceLang, string targetLang, string word)
        {
            var key = CreateTranslationKey(sourceLang, targetLang, word);
            return _translationCache.TryGetValue(key, out var translation) ? translation : null;
        }
        
        /// <summary>
        /// Add translation to FLYWEIGHT cache (shared across all instances)
        /// </summary>
        public void AddTranslation(string sourceLang, string targetLang, string word, string translation)
        {
            if (string.IsNullOrWhiteSpace(translation))
            {
                return;
            }
            
            var key = CreateTranslationKey(sourceLang, targetLang, word);
            var added = _translationCache.TryAdd(key, translation);
            
            if (added)
            {
                Console.WriteLine($"🪶 FLYWEIGHT cached: {word} ({sourceLang}) → {translation} ({targetLang})");
            }
        }
        
        /// <summary>
        /// Get cached transliteration or return null
        /// </summary>
        public string? GetTransliteration(string sourceLang, string word)
        {
            var key = CreateTransliterationKey(sourceLang, word);
            return _transliterationCache.TryGetValue(key, out var trans) ? trans : null;
        }
        
        /// <summary>
        /// Add transliteration to cache
        /// </summary>
        public void AddTransliteration(string sourceLang, string word, string transliteration)
        {
            if (string.IsNullOrWhiteSpace(transliteration))
            {
                return;
            }
            
            var key = CreateTransliterationKey(sourceLang, word);
            _transliterationCache.TryAdd(key, transliteration);
        }
        
        /// <summary>
        /// Get cached furigana (Japanese reading aid) or return null
        /// </summary>
        public string? GetFurigana(string word)
        {
            var key = CreateFuriganaKey(word);
            return _furiganaCache.TryGetValue(key, out var furigana) ? furigana : null;
        }
        
        /// <summary>
        /// Add furigana to cache
        /// </summary>
        public void AddFurigana(string word, string furigana)
        {
            if (string.IsNullOrWhiteSpace(furigana))
            {
                return;
            }
            
            var key = CreateFuriganaKey(word);
            _furiganaCache.TryAdd(key, furigana);
        }
        
        /// <summary>
        /// Batch get translations (returns only cached ones)
        /// </summary>
        public Dictionary<string, string> GetTranslations(
            string sourceLang, 
            string targetLang, 
            IEnumerable<string> words)
        {
            var results = new Dictionary<string, string>();
            
            foreach (var word in words)
            {
                var translation = GetTranslation(sourceLang, targetLang, word);
                if (translation != null)
                {
                    results[word] = translation;
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStats GetStats()
        {
            return new CacheStats
            {
                TranslationsCached = _translationCache.Count,
                TransliterationsCached = _transliterationCache.Count,
                FuriganaCached = _furiganaCache.Count,
                TotalEntries = _translationCache.Count + _transliterationCache.Count + _furiganaCache.Count + _dictionaryCache.Count
            };
        }
        
        /// <summary>
        /// Clear all caches
        /// </summary>
        public void Clear()
        {
            _translationCache.Clear();
            _transliterationCache.Clear();
            _furiganaCache.Clear();
            Console.WriteLine("🗑️ FLYWEIGHT cache cleared");
        }
        
        // ===== Private Key Generators =====
        
        private string CreateTranslationKey(string sourceLang, string targetLang, string word)
        {
            return $"{sourceLang.ToLower()}:{targetLang.ToLower()}:{word.ToLower()}";
        }
        
        private string CreateTransliterationKey(string sourceLang, string word)
        {
            return $"{sourceLang.ToLower()}:{word.ToLower()}";
        }
        
        private string CreateFuriganaKey(string word)
        {
            return $"ja:{word}";
        }

        private string CreateDictionaryKey(string lang, string word)
        {
            return $"{lang.ToLowerInvariant()}:{word.ToLowerInvariant()}";
        }

        // Dictionary flyweight methods
        public POT_SEM.Core.Models.DictionaryEntry? GetDictionaryEntry(string lang, string word)
        {
            var key = CreateDictionaryKey(lang, word);
            return _dictionaryCache.TryGetValue(key, out var entry) ? entry : null;
        }

        public void AddDictionaryEntry(string lang, string word, POT_SEM.Core.Models.DictionaryEntry entry)
        {
            if (entry == null) return;
            var key = CreateDictionaryKey(lang, word);
            _dictionaryCache.TryAdd(key, entry);
        }

        public async Task<Dictionary<string, POT_SEM.Core.Models.DictionaryEntry>> GetDictionaryEntriesBatchAsync(List<string> words, string lang)
        {
            var results = new Dictionary<string, POT_SEM.Core.Models.DictionaryEntry>();
            var toFetch = new List<string>();

            foreach (var word in words)
            {
                var key = CreateDictionaryKey(lang, word);
                if (_dictionaryCache.TryGetValue(key, out var cached))
                    results[word] = cached;
                else
                    toFetch.Add(word);
            }

            if (toFetch.Any() && _wiktionaryService != null)
            {
                var fetched = await _wiktionaryService.LookupBatchAsync(toFetch, lang).ConfigureAwait(false);
                foreach (var kv in fetched)
                {
                    var key = CreateDictionaryKey(lang, kv.Key);
                    _dictionaryCache[key] = kv.Value;
                    results[kv.Key] = kv.Value;
                }
            }

            return results;
        }
    }
    
    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStats
    {
        public int TranslationsCached { get; init; }
        public int TransliterationsCached { get; init; }
        public int FuriganaCached { get; init; }
        public int TotalEntries { get; init; }
        
        public override string ToString()
        {
            return $"Cache: {TotalEntries} total (T:{TranslationsCached}, TL:{TransliterationsCached}, F:{FuriganaCached})";
        }
    }
}