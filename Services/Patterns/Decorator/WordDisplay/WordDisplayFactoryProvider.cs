using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// Provides correct IWordDisplayFactory based on language (Singleton)
    /// </summary>
    public class WordDisplayFactoryProvider
    {
        private static readonly WordDisplayFactoryProvider _instance = new WordDisplayFactoryProvider();
        public static WordDisplayFactoryProvider Instance => _instance;
        
        private readonly Dictionary<string, IWordDisplayFactory> _factories;
        
        private WordDisplayFactoryProvider()
        {
            _factories = new Dictionary<string, IWordDisplayFactory>
            {
                { "en", EnglishWordDisplayFactory.Instance },
                { "sk", SlovakWordDisplayFactory.Instance },
                { "ar", ArabicWordDisplayFactory.Instance },
                { "ja", JapaneseWordDisplayFactory.Instance }
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
            
            return _factories["en"];
        }
        
    }
}