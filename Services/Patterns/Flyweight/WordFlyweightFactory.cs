using System.Collections.Concurrent;
using POT_SEM.Services.Patterns.Strategy;

namespace POT_SEM.Services.Patterns.Flyweight
{
    /// <summary>
    /// Manages pool of shared WordFlyweight objects
    /// Integrates with database for persistence
    /// Stores only word translations (intrinsic state)
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
                }
            }
        }
        
        /// <summary>
        /// Get translation synchronously
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
                
        private string CreateKey(string sourceLang, string targetLang, string normalized) =>
            $"{sourceLang.ToLower()}:{targetLang.ToLower()}:{normalized}";
    }
}
