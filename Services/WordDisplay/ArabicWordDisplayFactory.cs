using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.WordDisplay.Factories
{
    /// <summary>
    /// ABSTRACT FACTORY - Concrete Factory for Arabic
    /// Creates 3-layer word displays: Original + Transliteration + Translation
    /// </summary>
    public class ArabicWordDisplayFactory : IWordDisplayFactory
    {
        public string LanguageCode => "ar";
        
        public int LayerCount => 3;
        
        public IWordDisplay CreateWordDisplay(ProcessedWord word)
        {
            if (word.IsPunctuation)
            {
                return new BaseWordDisplay(word);
            }
            
            // Build: Base → Transliteration → Translation
            IWordDisplay display = new BaseWordDisplay(word);
            display = new TransliterationDecorator(display, word.Transliteration);
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
                "Original (Arabic)",
                "Transliteration (Latin)",
                "Translation"
            };
        }
    }
}