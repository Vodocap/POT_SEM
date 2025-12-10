using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// Concrete Factory for Slovak (Singleton)
    /// </summary>
    public class SlovakWordDisplayFactory : IWordDisplayFactory
    {
        private static readonly SlovakWordDisplayFactory _instance = new SlovakWordDisplayFactory();
        public static SlovakWordDisplayFactory Instance => _instance;
        
        private SlovakWordDisplayFactory() { }
        
        public string LanguageCode => "sk";
        
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
