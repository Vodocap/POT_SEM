using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Transliteration
{
    /// <summary>
    /// STRATEGY - Japanese to Romaji conversion
    /// Converts hiragana/katakana to Latin alphabet
    /// </summary>
    public class JapaneseRomajiService : ITransliterationService
    {
        public string ServiceName => "Japanese Romaji Converter";
        
        private static readonly Dictionary<string, string> HiraganaToRomaji = new()
        {
            // Vowels
            {"あ", "a"}, {"い", "i"}, {"う", "u"}, {"え", "e"}, {"お", "o"},
            
            // K-series
            {"か", "ka"}, {"き", "ki"}, {"く", "ku"}, {"け", "ke"}, {"こ", "ko"},
            
            // S-series
            {"さ", "sa"}, {"し", "shi"}, {"す", "su"}, {"せ", "se"}, {"そ", "so"},
            
            // T-series
            {"た", "ta"}, {"ち", "chi"}, {"つ", "tsu"}, {"て", "te"}, {"と", "to"},
            
            // N-series
            {"な", "na"}, {"に", "ni"}, {"ぬ", "nu"}, {"ね", "ne"}, {"の", "no"},
            
            // H-series
            {"は", "ha"}, {"ひ", "hi"}, {"ふ", "fu"}, {"へ", "he"}, {"ほ", "ho"},
            
            // M-series
            {"ま", "ma"}, {"み", "mi"}, {"む", "mu"}, {"め", "me"}, {"も", "mo"},
            
            // Y-series
            {"や", "ya"}, {"ゆ", "yu"}, {"よ", "yo"},
            
            // R-series
            {"ら", "ra"}, {"り", "ri"}, {"る", "ru"}, {"れ", "re"}, {"ろ", "ro"},
            
            // W-series
            {"わ", "wa"}, {"を", "wo"}, {"ん", "n"},
            
            // Common combinations
            {"が", "ga"}, {"ぎ", "gi"}, {"ぐ", "gu"}, {"げ", "ge"}, {"ご", "go"},
            {"ざ", "za"}, {"じ", "ji"}, {"ず", "zu"}, {"ぜ", "ze"}, {"ぞ", "zo"},
            {"だ", "da"}, {"ぢ", "ji"}, {"づ", "zu"}, {"で", "de"}, {"ど", "do"},
            {"ば", "ba"}, {"び", "bi"}, {"ぶ", "bu"}, {"べ", "be"}, {"ぼ", "bo"},
            {"ぱ", "pa"}, {"ぴ", "pi"}, {"ぷ", "pu"}, {"ぺ", "pe"}, {"ぽ", "po"}
        };
        
        public async Task<string?> TransliterateAsync(string text, string language)
        {
            if (language != "ja")
            {
                return null;
            }
            
            // Convert katakana to hiragana for consistent mapping
            var normalized = ConvertKatakanaToHiragana(text);

            var sb = new System.Text.StringBuilder(normalized.Length * 2);
            var mappedAny = false;

            for (int i = 0; i < normalized.Length; i++)
            {
                var ch = normalized[i].ToString();

                if (HiraganaToRomaji.TryGetValue(ch, out var romaji))
                {
                    sb.Append(romaji);
                    mappedAny = true;
                }
                else
                {
                    // skip kanji/unknown characters rather than echoing them
                }
            }

            if (!mappedAny)
            {
                // Nothing transliterable (likely pure kanji) — signal failure so caller doesn't store kanji as romaji
                return await Task.FromResult<string?>(null);
            }

            return await Task.FromResult(sb.ToString());
        }

        private static string ConvertKatakanaToHiragana(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new System.Text.StringBuilder(input.Length);
            foreach (var ch in input)
            {
                // Katakana block: U+30A0 - U+30FF, Hiragana block: U+3040 - U+309F
                if (ch >= '\u30A1' && ch <= '\u30F6')
                {
                    // shift to hiragana equivalent
                    var hiragana = (char)(ch - 0x60);
                    sb.Append(hiragana);
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }
        
        public bool SupportsLanguage(string language) => language == "ja";
    }
}