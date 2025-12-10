using System.Collections.Concurrent;
using POT_SEM.Services.Patterns.Strategy;

namespace POT_SEM.Services.Patterns.Flyweight
{
    /// <summary>
    /// FLYWEIGHT PATTERN (Gang of Four) - Factory
    /// Manages pool of shared WordFlyweight objects
    /// Integrates with database for persistence
    /// Stores ONLY word translations (intrinsic state)
    /// </summary>
    public class WordFlyweightFactory
    {
        private readonly ConcurrentDictionary<string, WordFlyweight> _flyweightPool = new();
        private readonly DatabaseTranslationService? _database;
        
        public WordFlyweightFactory(DatabaseTranslationService? database = null)
        {
            _database = database;
        }
        
        /// <summary>
        /// Get or create flyweight with database integration
        /// Auto-loads translation from DB if flyweight doesn't exist
        /// </summary>
        public async Task<WordFlyweight> GetOrCreateAsync(string text, string sourceLang, string targetLang)
        {
            var normalized = text.ToLower().Trim();
            var key = CreateKey(sourceLang, targetLang, normalized);
            
            if (_flyweightPool.TryGetValue(key, out var existingFlyweight))
            {
                existingFlyweight.LastAccessed = DateTime.UtcNow;
                return existingFlyweight;
            }
            
            var flyweight = new WordFlyweight(text, normalized, sourceLang, targetLang);
            
            // Auto-load from database
            if (_database != null)
            {
                try
                {
                    var dbTranslation = await _database.TranslateWordAsync(normalized, sourceLang, targetLang);
                    if (dbTranslation != null)
                    {
                        flyweight.Translation = dbTranslation;
                    }
                }
                catch
                {
                    // ignore database errors
                }
            }
            
            return _flyweightPool.GetOrAdd(key, flyweight);
        }
        
        /// <summary>
        /// Update flyweight translation and persist to database
        /// </summary>
        public async Task UpdateTranslationAsync(WordFlyweight flyweight, string translation)
        {
            flyweight.Translation = translation;
            
            // Auto-save to database
            if (_database != null)
            {
                try
                {
                    await _database.SaveTranslationAsync(
                        flyweight.Normalized,
                        translation,
                        flyweight.SourceLanguage,
                        flyweight.TargetLanguage,
                        null,
                        null);
                }
                catch
                {
                    // ignore database errors
                }
            }
        }
        
        /// <summary>
        /// Get translation synchronously (for cache compatibility)
        /// </summary>
        public string? GetTranslation(string sourceLang, string targetLang, string word)
        {
            var key = CreateKey(sourceLang, targetLang, word.ToLower().Trim());
            if (_flyweightPool.TryGetValue(key, out var flyweight))
            {
                flyweight.LastAccessed = DateTime.UtcNow;
                return flyweight.Translation;
            }
            return null;
        }
        
        /// <summary>
        /// Add translation synchronously (for cache compatibility)
        /// </summary>
        public void AddTranslation(string sourceLang, string targetLang, string word, string translation)
        {
            if (string.IsNullOrWhiteSpace(translation)) return;
            
            var normalized = word.ToLower().Trim();
            var key = CreateKey(sourceLang, targetLang, normalized);
            
            if (_flyweightPool.TryGetValue(key, out var existing))
            {
                existing.Translation = translation;
                existing.LastAccessed = DateTime.UtcNow;
            }
            else
            {
                _flyweightPool.TryAdd(key, new WordFlyweight(word, normalized, sourceLang, targetLang) { Translation = translation });
            }
        }
        
        /// <summary>
        /// Batch get or create flyweights with database integration
        /// </summary>
        public async Task<List<WordFlyweight>> GetOrCreateBatchAsync(IEnumerable<string> words, string sourceLang, string targetLang)
        {
            var flyweights = new List<WordFlyweight>();
            var wordsToFetch = new List<string>();
            
            foreach (var word in words)
            {
                var key = CreateKey(sourceLang, targetLang, word.ToLower().Trim());
                if (_flyweightPool.TryGetValue(key, out var existing))
                {
                    existing.LastAccessed = DateTime.UtcNow;
                    flyweights.Add(existing);
                }
                else
                {
                    wordsToFetch.Add(word);
                }
            }
            
            if (wordsToFetch.Any() && _database != null)
            {
                try
                {
                    var dbTranslations = await _database.TranslateBatchAsync(wordsToFetch.Select(w => w.ToLower().Trim()), sourceLang, targetLang);
                    foreach (var word in wordsToFetch)
                    {
                        var normalized = word.ToLower().Trim();
                        var flyweight = new WordFlyweight(word, normalized, sourceLang, targetLang);
                        if (dbTranslations.TryGetValue(normalized, out var translation))
                        {
                            flyweight.Translation = translation;
                        }
                        flyweights.Add(_flyweightPool.GetOrAdd(CreateKey(sourceLang, targetLang, normalized), flyweight));
                    }
                }
                catch
                {
                    foreach (var word in wordsToFetch)
                    {
                        var normalized = word.ToLower().Trim();
                        flyweights.Add(_flyweightPool.GetOrAdd(CreateKey(sourceLang, targetLang, normalized), new WordFlyweight(word, normalized, sourceLang, targetLang)));
                    }
                }
            }
            else
            {
                foreach (var word in wordsToFetch)
                {
                    var normalized = word.ToLower().Trim();
                    flyweights.Add(_flyweightPool.GetOrAdd(CreateKey(sourceLang, targetLang, normalized), new WordFlyweight(word, normalized, sourceLang, targetLang)));
                }
            }
            
            return flyweights;
        }
        
        public void Clear() => _flyweightPool.Clear();
        
        private string CreateKey(string sourceLang, string targetLang, string normalized) =>
            $"{sourceLang.ToLower()}:{targetLang.ToLower()}:{normalized}";
    }
}
