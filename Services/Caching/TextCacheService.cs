using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using System.Collections.Concurrent;

namespace POT_SEM.Services.Caching
{
    /// <summary>
    /// In-memory cache for texts
    /// Thread-safe using ConcurrentDictionary
    /// </summary>
    public class TextCacheService : ITextCacheService
    {
        // Cache structure: "en_Beginner" -> List<Text>
        private readonly ConcurrentDictionary<string, List<Text>> _cache;
        private DateTime _lastPreloadTime;
        
        public TextCacheService()
        {
            _cache = new ConcurrentDictionary<string, List<Text>>();
            _lastPreloadTime = DateTime.MinValue;
        }
        
        public List<Text>? GetCachedTexts(string languageCode, DifficultyLevel difficulty)
        {
            var key = GetCacheKey(languageCode, difficulty);
            
            if (_cache.TryGetValue(key, out var texts))
            {
                Console.WriteLine($"💾 Cache HIT: {key} ({texts.Count} texts)");
                return texts.ToList(); // Return copy to avoid mutation
            }
            
            Console.WriteLine($"❌ Cache MISS: {key}");
            return null;
        }
        
        public void CacheTexts(string languageCode, DifficultyLevel difficulty, List<Text> texts)
        {
            var key = GetCacheKey(languageCode, difficulty);
            
            _cache[key] = texts.ToList(); // Store copy
            _lastPreloadTime = DateTime.UtcNow;
            
            Console.WriteLine($"💾 Cached: {key} ({texts.Count} texts)");
        }
        
        public bool IsCached(string languageCode, DifficultyLevel difficulty)
        {
            var key = GetCacheKey(languageCode, difficulty);
            return _cache.ContainsKey(key);
        }
        
        public void ClearCache()
        {
            _cache.Clear();
            Console.WriteLine("🗑️ Cache cleared");
        }
        
        public CacheStats GetStats()
        {
            var stats = new CacheStats
            {
                TotalCachedTexts = _cache.Values.Sum(list => list.Count),
                CachedLanguages = _cache.Keys.Select(k => k.Split('_')[0]).Distinct().Count(),
                CachedDifficulties = _cache.Keys.Select(k => k.Split('_')[1]).Distinct().Count(),
                LastPreloadTime = _lastPreloadTime
            };
            
            return stats;
        }
        
        private string GetCacheKey(string languageCode, DifficultyLevel difficulty)
        {
            return $"{languageCode}_{difficulty}";
        }
    }
}