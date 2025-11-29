using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Transliteration
{
    /// <summary>
    /// STRATEGY - Arabic to Latin script conversion
    /// </summary>
    public class ArabicTransliterationService : ITransliterationService
    {
        public string ServiceName => "Arabic Transliteration";
        
        private static readonly Dictionary<string, string> ArabicToLatin = new()
        {
            {"ا", "a"}, {"أ", "a"}, {"إ", "i"}, {"آ", "aa"},
            {"ب", "b"}, {"ت", "t"}, {"ث", "th"},
            {"ج", "j"}, {"ح", "h"}, {"خ", "kh"},
            {"د", "d"}, {"ذ", "dh"},
            {"ر", "r"}, {"ز", "z"},
            {"س", "s"}, {"ش", "sh"},
            {"ص", "s"}, {"ض", "d"},
            {"ط", "t"}, {"ظ", "z"},
            {"ع", "'"}, {"غ", "gh"},
            {"ف", "f"}, {"ق", "q"},
            {"ك", "k"}, {"ل", "l"},
            {"م", "m"}, {"ن", "n"},
            {"ه", "h"}, {"و", "w"},
            {"ي", "y"}, {"ى", "a"},
            {"ة", "h"}
        };
        
        public async Task<string?> TransliterateAsync(string text, string language)
        {
            if (language != "ar")
            {
                return null;
            }
            
            var result = "";
            
            foreach (var ch in text)
            {
                var key = ch.ToString();
                if (ArabicToLatin.ContainsKey(key))
                {
                    result += ArabicToLatin[key];
                }
                else if (char.IsWhiteSpace(ch))
                {
                    result += " ";
                }
            }
            
            return await Task.FromResult(result.Trim());
        }
        
        public bool SupportsLanguage(string language) => language == "ar";
    }
}