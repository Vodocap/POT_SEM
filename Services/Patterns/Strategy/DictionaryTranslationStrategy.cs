using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Dictionary;

namespace POT_SEM.Services.Patterns.Strategy
{
    /// <summary>
    /// Dictionary-based translation using AI API for word meanings.
    /// </summary>
    public class DictionaryTranslationStrategy : ITranslationStrategy
    {
        private readonly ApiDictionaryService _dictionaryService;

        public string StrategyName => "Dictionary (AI API)";

        public DictionaryTranslationStrategy(ApiDictionaryService dictionaryService)
        {
            _dictionaryService = dictionaryService ?? throw new ArgumentNullException(nameof(dictionaryService));
        }

        public async Task<string?> TranslateWordAsync(string word, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return null;
            }

            var entry = await _dictionaryService.LookupAsync(word, sourceLang, targetLang);
            
            if (entry?.Meanings != null && entry.Meanings.Count > 0)
            {
                return string.Join("; ", entry.Meanings);
            }

            return null;
        }

        public Task<string?> TranslateSentenceAsync(string sentence, string sourceLang, string targetLang)
        {
            // Dictionary does NOT translate sentences
            return Task.FromResult<string?>(null);
        }

        public async Task<Dictionary<string, string>> TranslateBatchAsync(
            IEnumerable<string> words,
            string sourceLang,
            string targetLang)
        {
            var results = new Dictionary<string, string>();
            var wordsList = words.ToList();

            var entries = await _dictionaryService.LookupBatchAsync(wordsList, sourceLang, targetLang);

            foreach (var kvp in entries)
            {
                if (kvp.Value.Meanings != null && kvp.Value.Meanings.Count > 0)
                {
                    results[kvp.Key] = string.Join("; ", kvp.Value.Meanings);
                }
            }

            return results;
        }
    }
}
