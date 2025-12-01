using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// ABSTRACT FACTORY - Concrete Factory for English
    /// Creates 2-layer word displays: Original + Translation
    /// </summary>
    public class EnglishWordDisplayFactory : IWordDisplayFactory
    {
        public string LanguageCode => "en";
        
        public int LayerCount => 2;
        
        public IWordDisplay CreateWordDisplay(ProcessedWord word)
        {
            // Punctuation: no decorators
            if (word.IsPunctuation)
            {
                return new BaseWordDisplay(word);
            }
            
            // Build: Base → Translation
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
    
    /// <summary>
    /// ABSTRACT FACTORY - Concrete Factory for Slovak
    /// Same structure as English (2 layers)
    /// </summary>
    public class SlovakWordDisplayFactory : IWordDisplayFactory
    {
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