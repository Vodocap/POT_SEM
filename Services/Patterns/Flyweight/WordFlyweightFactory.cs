using System.Collections.Concurrent;
using POT_SEM.Services.Patterns.Strategy;

namespace POT_SEM.Services.Patterns.Flyweight
{
    /// <summary>
    /// FLYWEIGHT PATTERN (Gang of Four) - Factory
    /// Manages pool of shared WordFlyweight objects
    /// Ensures same word = same flyweight instance (memory sharing)
    /// Integrates with database to load/persist translations
    /// </summary>
    public class WordFlyweightFactory
    {
        // Pool of shared flyweights: Key = "sourceLang:targetLang:normalizedWord"
        private readonly ConcurrentDictionary<string, WordFlyweight> _flyweightPool = new();
        
        // Database service for persistent storage
        private readonly DatabaseTranslationService? _database;
        
        // API dictionary service for fetching meanings
        private readonly POT_SEM.Services.Dictionary.ApiDictionaryService? _apiDictionaryService;
        
        // Statistics
        private int _flyweightsCreated = 0;
        private int _flyweightsReused = 0;
        private int _databaseHits = 0;
        private int _databaseMisses = 0;
        
        public WordFlyweightFactory(
            DatabaseTranslationService? database = null,
            POT_SEM.Services.Dictionary.ApiDictionaryService? apiDictionaryService = null)
        {
            _database = database;
            _apiDictionaryService = apiDictionaryService;
        }
        
        /// <summary>
        /// Get or create flyweight for a word
        /// Gang of Four: Returns THE SAME INSTANCE for identical words
        /// Checks database if flyweight doesn't exist in memory
        /// </summary>
        public async Task<WordFlyweight> GetFlyweightAsync(
            string text, 
            string sourceLang, 
            string targetLang)
        {
            var normalized = text.ToLower().Trim();
            var key = CreateKey(sourceLang, targetLang, normalized);
            
            // Check if flyweight already exists (REUSE)
            if (_flyweightPool.TryGetValue(key, out var existingFlyweight))
            {
                _flyweightsReused++;
                existingFlyweight.LastAccessed = DateTime.UtcNow;
                return existingFlyweight;
            }
            
            // Create new flyweight
            var flyweight = new WordFlyweight(text, normalized, sourceLang, targetLang);
            _flyweightsCreated++;
            
            // Try to load translation from database
            if (_database != null)
            {
                try
                {
                    var dbTranslation = await _database.TranslateWordAsync(normalized, sourceLang, targetLang);
                    if (dbTranslation != null)
                    {
                        flyweight.Translation = dbTranslation;
                        _databaseHits++;
                    }
                    else
                    {
                        _databaseMisses++;
                    }
                }
                catch
                {
                    _databaseMisses++;
                }
            }
            
            // Add to pool (or get existing if another thread added it)
            var actualFlyweight = _flyweightPool.GetOrAdd(key, flyweight);
            
            return actualFlyweight;
        }
        
        /// <summary>
        /// Update flyweight with translation data
        /// This updates the shared instance, so all references see the new data
        /// Also persists to database
        /// </summary>
        public async Task UpdateFlyweightAsync(
            WordFlyweight flyweight, 
            string? translation = null,
            string? transliteration = null,
            string? furigana = null,
            POT_SEM.Core.Models.DictionaryEntry? dictionaryEntry = null)
        {
            if (translation != null)
                flyweight.Translation = translation;
            
            if (transliteration != null)
                flyweight.Transliteration = transliteration;
            
            if (furigana != null)
                flyweight.Furigana = furigana;
            
            if (dictionaryEntry != null)
                flyweight.DictionaryEntry = dictionaryEntry;
            
            // Persist to database if translation was added
            if (_database != null && translation != null)
            {
                try
                {
                    await _database.SaveTranslationAsync(
                        flyweight.Normalized,
                        translation,
                        flyweight.SourceLanguage,
                        flyweight.TargetLanguage,
                        transliteration,
                        furigana);
                }
                catch
                {
                    // Failed to persist, flyweight still updated in memory
                }
            }
        }
        
        /// <summary>
        /// Get dictionary entry for a flyweight
        /// </summary>
        public async Task<POT_SEM.Core.Models.DictionaryEntry?> GetDictionaryEntryAsync(
            WordFlyweight flyweight)
        {
            if (flyweight.DictionaryEntry != null)
                return flyweight.DictionaryEntry;
            
            if (_apiDictionaryService == null)
                return null;
            
            try
            {
                var entries = await _apiDictionaryService.LookupBatchAsync(
                    new List<string> { flyweight.Normalized },
                    flyweight.SourceLanguage,
                    flyweight.TargetLanguage);
                
                if (entries.TryGetValue(flyweight.Normalized, out var entry))
                {
                    flyweight.DictionaryEntry = entry;
                    return entry;
                }
            }
            catch
            {
                // Failed to fetch dictionary entry
            }
            
            return null;
        }
        
        /// <summary>
        /// Get batch of flyweights efficiently
        /// Minimizes database queries by batching
        /// </summary>
        public async Task<List<WordFlyweight>> GetFlyweightsBatchAsync(
            IEnumerable<string> words,
            string sourceLang,
            string targetLang)
        {
            var flyweights = new List<WordFlyweight>();
            var wordsToFetch = new List<string>();
            
            // Check which words already have flyweights
            foreach (var word in words)
            {
                var normalized = word.ToLower().Trim();
                var key = CreateKey(sourceLang, targetLang, normalized);
                
                if (_flyweightPool.TryGetValue(key, out var existing))
                {
                    _flyweightsReused++;
                    existing.LastAccessed = DateTime.UtcNow;
                    flyweights.Add(existing);
                }
                else
                {
                    wordsToFetch.Add(word);
                }
            }
            
            // Fetch translations for new words from database in batch
            if (wordsToFetch.Any() && _database != null)
            {
                try
                {
                    var dbTranslations = await _database.TranslateBatchAsync(
                        wordsToFetch.Select(w => w.ToLower().Trim()),
                        sourceLang,
                        targetLang);
                    
                    foreach (var word in wordsToFetch)
                    {
                        var normalized = word.ToLower().Trim();
                        var flyweight = new WordFlyweight(word, normalized, sourceLang, targetLang);
                        _flyweightsCreated++;
                        
                        if (dbTranslations.TryGetValue(normalized, out var translation))
                        {
                            flyweight.Translation = translation;
                            _databaseHits++;
                        }
                        else
                        {
                            _databaseMisses++;
                        }
                        
                        var key = CreateKey(sourceLang, targetLang, normalized);
                        var actualFlyweight = _flyweightPool.GetOrAdd(key, flyweight);
                        flyweights.Add(actualFlyweight);
                    }
                }
                catch
                {
                    // Database batch failed, create flyweights without translations
                    foreach (var word in wordsToFetch)
                    {
                        var normalized = word.ToLower().Trim();
                        var flyweight = new WordFlyweight(word, normalized, sourceLang, targetLang);
                        _flyweightsCreated++;
                        _databaseMisses++;
                        
                        var key = CreateKey(sourceLang, targetLang, normalized);
                        var actualFlyweight = _flyweightPool.GetOrAdd(key, flyweight);
                        flyweights.Add(actualFlyweight);
                    }
                }
            }
            else
            {
                // No database, create flyweights without translations
                foreach (var word in wordsToFetch)
                {
                    var normalized = word.ToLower().Trim();
                    var flyweight = new WordFlyweight(word, normalized, sourceLang, targetLang);
                    _flyweightsCreated++;
                    
                    var key = CreateKey(sourceLang, targetLang, normalized);
                    var actualFlyweight = _flyweightPool.GetOrAdd(key, flyweight);
                    flyweights.Add(actualFlyweight);
                }
            }
            
            return flyweights;
        }
        
        /// <summary>
        /// Get factory statistics
        /// </summary>
        public FlyweightStats GetStats()
        {
            return new FlyweightStats
            {
                TotalFlyweights = _flyweightPool.Count,
                FlyweightsCreated = _flyweightsCreated,
                FlyweightsReused = _flyweightsReused,
                DatabaseHits = _databaseHits,
                DatabaseMisses = _databaseMisses,
                ReuseRate = _flyweightsCreated > 0 
                    ? (double)_flyweightsReused / (_flyweightsCreated + _flyweightsReused) 
                    : 0,
                DatabaseHitRate = (_databaseHits + _databaseMisses) > 0
                    ? (double)_databaseHits / (_databaseHits + _databaseMisses)
                    : 0
            };
        }
        
        /// <summary>
        /// Clear flyweight pool (for testing or memory management)
        /// </summary>
        public void Clear()
        {
            _flyweightPool.Clear();
            _flyweightsCreated = 0;
            _flyweightsReused = 0;
            _databaseHits = 0;
            _databaseMisses = 0;
        }
        
        private string CreateKey(string sourceLang, string targetLang, string normalized)
        {
            return $"{sourceLang.ToLower()}:{targetLang.ToLower()}:{normalized}";
        }
    }
    
    /// <summary>
    /// Flyweight factory statistics
    /// </summary>
    public class FlyweightStats
    {
        public int TotalFlyweights { get; init; }
        public int FlyweightsCreated { get; init; }
        public int FlyweightsReused { get; init; }
        public int DatabaseHits { get; init; }
        public int DatabaseMisses { get; init; }
        public double ReuseRate { get; init; }
        public double DatabaseHitRate { get; init; }
        
        public override string ToString()
        {
            return $"Flyweights: {TotalFlyweights} total, {FlyweightsCreated} created, {FlyweightsReused} reused " +
                   $"(Reuse: {ReuseRate:P1}, DB Hit: {DatabaseHitRate:P1})";
        }
    }
}
