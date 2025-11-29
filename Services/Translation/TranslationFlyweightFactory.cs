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
                TotalEntries = _translationCache.Count + _transliterationCache.Count + _furiganaCache.Count
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