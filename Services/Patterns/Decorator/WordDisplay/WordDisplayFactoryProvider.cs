using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// Provides correct IWordDisplayFactory based on language
    /// This is NOT part of Abstract Factory pattern - just a helper
    /// </summary>
    public class WordDisplayFactoryProvider
    {
        private readonly Dictionary<string, IWordDisplayFactory> _factories;
        
        public WordDisplayFactoryProvider()
        {
            // Register all concrete factories
            _factories = new Dictionary<string, IWordDisplayFactory>
            {
                { "en", new EnglishWordDisplayFactory() },
                { "sk", new SlovakWordDisplayFactory() },
                { "ar", new ArabicWordDisplayFactory() },
                { "ja", new JapaneseWordDisplayFactory() }
            };
        }
        
        /// <summary>
        /// Get factory for specific language
        /// </summary>
        public IWordDisplayFactory GetFactory(string languageCode)
        {
            var lang = languageCode.ToLower();
            
            if (_factories.TryGetValue(lang, out var factory))
            {
                return factory;
            }
            
            // Default to English factory
            return _factories["en"];
        }
        
        /// <summary>
        /// Get all available factories
        /// </summary>
        public IEnumerable<IWordDisplayFactory> GetAllFactories()
        {
            return _factories.Values;
        }
        
        /// <summary>
        /// Get supported languages
        /// </summary>
        public IEnumerable<string> GetSupportedLanguages()
        {
            return _factories.Keys;
        }
    }
}