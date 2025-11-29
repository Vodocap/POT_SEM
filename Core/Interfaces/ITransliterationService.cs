namespace POT_SEM.Core.Interfaces
{
    /// <summary>
    /// STRATEGY PATTERN - Transliteration service
    /// Converts non-Latin scripts to Latin alphabet
    /// </summary>
    public interface ITransliterationService
    {
        string ServiceName { get; }
        
        /// <summary>
        /// Transliterate word to Latin script
        /// </summary>
        Task<string?> TransliterateAsync(string text, string language);
        
        /// <summary>
        /// Check if language is supported
        /// </summary>
        bool SupportsLanguage(string language);
    }
}