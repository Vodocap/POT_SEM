using POT_SEM.Core.Models;

namespace POT_SEM.Core.Interfaces
{
    /// <summary>
    /// ABSTRACT FACTORY PATTERN - Interface
    /// Defines contract for creating word displays for specific language
    /// Each concrete factory creates displays with appropriate layers
    /// </summary>
    public interface IWordDisplayFactory
    {
        /// <summary>
        /// Language code this factory handles
        /// </summary>
        string LanguageCode { get; }
        
        /// <summary>
        /// Number of display layers this factory creates
        /// </summary>
        int LayerCount { get; }
        
        /// <summary>
        /// Create word display with language-specific decorators
        /// </summary>
        IWordDisplay CreateWordDisplay(ProcessedWord word);
        
        /// <summary>
        /// Create displays for entire sentence
        /// </summary>
        List<IWordDisplay> CreateSentenceDisplays(ProcessedSentence sentence);
        
        /// <summary>
        /// Get layer names for this language
        /// </summary>
        List<string> GetLayerNames();
    }
}