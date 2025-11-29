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
            
            var result = "";
            
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i].ToString();
                
                if (HiraganaToRomaji.ContainsKey(ch))
                {
                    result += HiraganaToRomaji[ch];
                }
                else
                {
                    result += ch; // Keep kanji/katakana as is
                }
            }
            
            return await Task.FromResult(result);
        }
        
        public bool SupportsLanguage(string language) => language == "ja";
    }
}