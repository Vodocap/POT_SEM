using POT_SEM.Core.Models;

// Core/Interfaces/ILanguageTextSource.cs
namespace POT_SEM.Core.Interfaces
{
    /// <summary>
    /// BRIDGE IMPLEMENTATION INTERFACE
    /// Reprezentuje zdroj textov v konkrétnom jazyku
    /// Deleguje získavanie textov na ITextFetchStrategy
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