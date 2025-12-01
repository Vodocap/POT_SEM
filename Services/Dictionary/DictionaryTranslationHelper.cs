using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.ChainOfResponsibility;

namespace POT_SEM.Services.Dictionary
{
    /// <summary>
    /// Helper to create joined dictionary-meaning translation strings and persist them when possible.
    /// </summary>
    public class DictionaryTranslationHelper
    {
        private readonly ChainedTranslationService? _chained;

        public DictionaryTranslationHelper(ChainedTranslationService? chained = null)
        {
            _chained = chained;
        }

        public string CreateJoinedMeanings(DictionaryEntry entry)
        {
            if (entry == null) return string.Empty;
            if (entry.Meanings == null || entry.Meanings.Count == 0) return string.Empty;
            return string.Join("; ", entry.Meanings.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()));
        }

        public async Task PersistIfPossibleAsync(string originalWord, string joinedMeanings, string srcLang, string tgtLang, string? transliteration = null, string? furigana = null)
        {
            if (_chained == null) return;
            try
            {
                await _chained.SaveTranslationToDatabaseAsync(originalWord, joinedMeanings, srcLang, tgtLang, transliteration, furigana);
            }
            catch
            {
                // Failed to persist, continue
            }
        }
    }
}
