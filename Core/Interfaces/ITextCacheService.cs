using POT_SEM.Core.Models;

namespace POT_SEM.Core.Interfaces
{
    /// <summary>
    /// Cache service for preloaded texts
    /// </summary>
    public interface ITextCacheService
    {
        /// <summary>
        /// Get cached texts for language + difficulty
        /// </summary>
        List<Text>? GetCachedTexts(string languageCode, DifficultyLevel difficulty);
        
        /// <summary>
        /// Store texts in cache
        /// </summary>
        void CacheTexts(string languageCode, DifficultyLevel difficulty, List<Text> texts);
        
        /// <summary>
        /// Check if cache has data for this combination
        /// </summary>
        bool IsCached(string languageCode, DifficultyLevel difficulty);
        
        /// <summary>
        /// Clear all cache
        /// </summary>
        void ClearCache();
        
        /// <summary>
        /// Get cache statistics
        /// </summary>
        CacheStats GetStats();
    }
    
    public class CacheStats
    {
        public int TotalCachedTexts { get; set; }
        public int CachedLanguages { get; set; }
        public int CachedDifficulties { get; set; }
        public DateTime LastPreloadTime { get; set; }
    }
}