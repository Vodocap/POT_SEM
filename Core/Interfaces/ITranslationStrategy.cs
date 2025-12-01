namespace POT_SEM.Core.Interfaces
{
    /// <summary>
    /// STRATEGY PATTERN - Interface for different translation implementations
    /// Allows switching between API, Database, or other translation sources
    /// </summary>
    public interface ITranslationStrategy
    {
        /// <summary>
        /// Name of this strategy
        /// </summary>
        string StrategyName { get; }
        
        /// <summary>
        /// Translate a single word
        /// </summary>
        Task<string?> TranslateWordAsync(string word, string sourceLang, string targetLang);
        
        /// <summary>
        /// Translate a full sentence (preserves context)
        /// </summary>
        Task<string?> TranslateSentenceAsync(string sentence, string sourceLang, string targetLang);
        
        /// <summary>
        /// Batch translate multiple words (more efficient)
        /// Returns: original word → translation
        /// </summary>
        Task<Dictionary<string, string>> TranslateBatchAsync(
            IEnumerable<string> words, 
            string sourceLang, 
            string targetLang);
    }
}