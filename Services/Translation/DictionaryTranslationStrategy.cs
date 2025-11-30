using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Dictionary;

namespace POT_SEM.Services.Translation
{
    /// <summary>
    /// STRATEGY PATTERN - Dictionary-based translation (Wiktionary)
    /// Provides word meanings from dictionary. Does NOT translate sentences (returns null).
    /// </summary>
    public class DictionaryTranslationStrategy : ITranslationStrategy
    {
        private readonly TranslationFlyweightFactory _flyweight;
        private readonly DictionaryTranslationHelper? _helper;

        public string StrategyName => "Dictionary (Wiktionary)";

        public DictionaryTranslationStrategy(
            TranslationFlyweightFactory flyweight,
            DictionaryTranslationHelper? helper = null)
        {
            _flyweight = flyweight;
            _helper = helper;
        }

        public async Task<string?> TranslateWordAsync(string word, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return null;
            }

            Console.WriteLine($"📚 DICTIONARY lookup: '{word}' (target lang: {targetLang})");

            // Check flyweight dictionary cache first
            var dictEntry = _flyweight.GetDictionaryEntry(targetLang, word);
            if (dictEntry == null)
            {
                // Fetch from Wiktionary if not cached
                var fetched = await _flyweight.GetDictionaryEntriesBatchAsync(new List<string> { word }, targetLang);
                fetched.TryGetValue(word, out dictEntry);
            }

            if (dictEntry != null && dictEntry.Meanings?.Count > 0)
            {
                var joined = _helper != null 
                    ? _helper.CreateJoinedMeanings(dictEntry) 
                    : string.Join("; ", dictEntry.Meanings);

                Console.WriteLine($"   ✅ DICTIONARY HIT: {dictEntry.Meanings.Count} meanings");
                return joined;
            }

            Console.WriteLine($"   ❌ DICTIONARY MISS");
            return null;
        }

        public Task<string?> TranslateSentenceAsync(string sentence, string sourceLang, string targetLang)
        {
            // Dictionary does NOT translate sentences (no context)
            return Task.FromResult<string?>(null);
        }

        public async Task<Dictionary<string, string>> TranslateBatchAsync(
            IEnumerable<string> words,
            string sourceLang,
            string targetLang)
        {
            var results = new Dictionary<string, string>();
            var wordsList = words.ToList();

            Console.WriteLine($"📚 DICTIONARY batch: {wordsList.Count} words");

            // Fetch all dictionary entries in one batch
            var entries = await _flyweight.GetDictionaryEntriesBatchAsync(wordsList, targetLang);

            foreach (var word in wordsList)
            {
                if (entries.TryGetValue(word, out var entry) && entry.Meanings?.Count > 0)
                {
                    var joined = _helper != null
                        ? _helper.CreateJoinedMeanings(entry)
                        : string.Join("; ", entry.Meanings);

                    results[word] = joined;
                }
            }

            Console.WriteLine($"   📚 DICTIONARY: {results.Count}/{wordsList.Count} found");

            return results;
        }
    }
}
