using POT_SEM.Core.Models;

namespace POT_SEM.Core.Interfaces
{
    /// <summary>
    /// BRIDGE IMPLEMENTATION INTERFACE
    /// Reprezentuje zdroj textov v konkrétnom jazyku
    /// </summary>
    public interface ILanguageTextSource
    {
        string LanguageCode { get; }
        string LanguageName { get; }
        
        Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria);
        bool SupportsDifficulty(DifficultyLevel level);
        Task<List<string>> GetAvailableTopicsAsync();
    }
}