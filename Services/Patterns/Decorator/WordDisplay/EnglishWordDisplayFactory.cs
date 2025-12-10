using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// Concrete Factory for English (Singleton)
    /// Creates 2-layer word displays: Original + Translation
    /// </summary>
    public class EnglishWordDisplayFactory : IWordDisplayFactory
    {
        private static readonly EnglishWordDisplayFactory _instance = new EnglishWordDisplayFactory();
        public static EnglishWordDisplayFactory Instance => _instance;
        
        private EnglishWordDisplayFactory() { }
        
        public string LanguageCode => "en";
        
        public int LayerCount => 2;
        
        public IWordDisplay CreateWordDisplay(ProcessedWord word)
        {
            if (word.IsPunctuation)
            {
                return new BaseWordDisplay(word);
            }
            
            IWordDisplay display = new BaseWordDisplay(word);
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
                "Original",
                "Translation"
            };
        }
    }
}