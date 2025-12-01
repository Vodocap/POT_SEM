using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// ABSTRACT FACTORY - Concrete Factory for Japanese
    /// Creates 4-layer word displays: Original + Furigana + Romaji + Translation
    /// </summary>
    public class JapaneseWordDisplayFactory : IWordDisplayFactory
    {
        public string LanguageCode => "ja";
        
        public int LayerCount => 4;
        
        public IWordDisplay CreateWordDisplay(ProcessedWord word)
        {
            if (word.IsPunctuation)
            {
                return new BaseWordDisplay(word);
            }
            
            // Build: Base → Furigana → Romaji → Translation
            IWordDisplay display = new BaseWordDisplay(word);
            display = new FuriganaDecorator(display, word.Furigana);
            display = new RomajiDecorator(display, word.Transliteration);
            display = new TranslationDecorator(display, word.Translation);
            
            return display;
        }
        
        public List<IWordDisplay> CreateSentenceDisplays(ProcessedSentence sentence)
        {
            return sentence.Words
                .Select(CreateWordDisplay)
                .ToList();
        }
        
        public List<string> GetLayerNames()
        {
            return new List<string>
            {
                "Original (Kanji)",
                "Furigana (Hiragana)",
                "Romaji (Latin)",
                "Translation"
            };
        }
    }
}