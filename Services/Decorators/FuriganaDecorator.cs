using POT_SEM.Core.Models;

namespace POT_SEM.Services.Decorators
{
    /// <summary>
    /// Simple Furigana decorator (MVP)
    /// Uses a small static dictionary to map common kanji words to hiragana readings.
    /// Production should use Kuroshiro.js via JSInterop or a proper dictionary.
    /// </summary>
    public class FuriganaDecorator
    {
        private static readonly Dictionary<string, string> _staticReadings = new()
        {
            ["東京"] = "とうきょう",
            ["日本"] = "にほん",
            ["学生"] = "がくせい",
            ["湯倉神社"] = "ゆくらじんじゃ",
            ["東郷村"] = "とうごうむら",
            ["カール・レムリ"] = "かーる・れむり"
        };

        public FuriganaDecorator()
        {
        }

        public Task<ProcessedText> DecorateTextAsync(ProcessedText text)
        {
            if (text.SourceLanguage?.ToLower() != "ja")
            {
                return Task.FromResult(text);
            }

            foreach (var sentence in text.Sentences)
            {
                foreach (var word in sentence.Words)
                {
                    if (word.IsPunctuation) continue;

                    // Try exact match first
                    if (_staticReadings.TryGetValue(word.Original, out var reading))
                    {
                        word.Furigana = reading;
                        // Prefer any existing transliteration produced earlier in the pipeline.
                        // We do not generate romaji here; RomajiDecorator already handles transliteration.
                        if (!string.IsNullOrEmpty(word.Transliteration))
                        {
                            // keep existing transliteration
                        }

                        word.Metadata["hasFurigana"] = true;
                        continue;
                    }

                    // Fallback: if contains kanji characters, try JS interop service if available
                    if (word.Original.Any(c => c >= '\u4E00' && c <= '\u9FFF'))
                    {
                        // Not found — mark as pending for client-side processing
                        word.Metadata["hasFurigana"] = false;
                    }
                }
            }

            return Task.FromResult(text);
        }

        // Note: romaji generation is intentionally omitted here. Use RomajiDecorator or a JSInterop-based
        // approach (Kuroshiro.js) for accurate transliteration.
    }
}
