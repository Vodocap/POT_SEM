using POT_SEM.Services.TextProviders;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Linq;

namespace POT_SEM.Services.Patterns.Factory
{
    /// <summary>
    /// Factory Method pattern for creating TextProvider instances with fluent configuration
    /// </summary>
    public class TextProviderFactory
    {
        private readonly LanguageSourceFactory _languageFactory;
        private readonly ITopicGenerationStrategy _topicStrategy;
        private readonly ITextCacheService? _cacheService;

        private DifficultyLevel? _difficulty;
        private ILanguageTextSource? _languageSource;

        // Language factory method registry
        private readonly Dictionary<string, Func<ITopicGenerationStrategy, ILanguageTextSource>> _languageRegistry;

        // Provider factory method registry
        private readonly Dictionary<DifficultyLevel, Func<ILanguageTextSource, ITextCacheService?, TextProvider>> _providerRegistry;
        
        // SINGLETON PATTERN - Cache of provider instances
        // Key: "languageCode:difficulty" (e.g. "en:Beginner")
        private readonly Dictionary<string, TextProvider> _providerCache = new();

        public TextProviderFactory(
            LanguageSourceFactory languageFactory,
            ITopicGenerationStrategy topicStrategy,
            ITextCacheService? cacheService = null)
        {
            _languageFactory = languageFactory;
            _topicStrategy = topicStrategy;
            _cacheService = cacheService;

            // Initialize language registry
            _languageRegistry = new Dictionary<string, Func<ITopicGenerationStrategy, ILanguageTextSource>>
            {
                ["en"] = _languageFactory.CreateEnglishSource,
                ["sk"] = _languageFactory.CreateSlovakSource,
                ["ar"] = _languageFactory.CreateArabicSource,
                ["ja"] = _languageFactory.CreateJapaneseSource
            };

            // Initialize provider registry
            _providerRegistry = new Dictionary<DifficultyLevel, Func<ILanguageTextSource, ITextCacheService?, TextProvider>>
            {
                [DifficultyLevel.Beginner] = (source, cache) => new BeginnerTextProvider(source, cache),
                [DifficultyLevel.Intermediate] = (source, cache) => new IntermediateTextProvider(source, cache),
                [DifficultyLevel.Advanced] = (source, cache) => new AdvancedTextProvider(source, cache)
            };
        }

        public TextProviderFactory ForLanguage(string languageCode)
        {
            string normalizedCode = languageCode.ToLower();

            if (!_languageRegistry.ContainsKey(normalizedCode))
            {
                throw new ArgumentException(
                    $"Unsupported language: {languageCode}. " +
                    $"Supported languages: {string.Join(", ", _languageRegistry.Keys)}"
                );
            }

            _languageSource = _languageRegistry[normalizedCode](_topicStrategy);

            return this;
        }

        public TextProviderFactory ForCustomSource(ILanguageTextSource source)
        {
            _languageSource = source;
            return this;
        }

        public TextProviderFactory ForDifficulty(DifficultyLevel difficulty)
        {
            _difficulty = difficulty;
            return this;
        }

        public TextProvider Build()
        {
            if (_languageSource == null)
            {
                throw new InvalidOperationException("Language source not specified. Call ForLanguage() or ForCustomSource() first.");
            }

            if (!_difficulty.HasValue)
            {
                throw new InvalidOperationException("Difficulty not specified. Call ForDifficulty() first.");
            }

            if (!_providerRegistry.ContainsKey(_difficulty.Value))
            {
                throw new ArgumentException($"Unsupported difficulty: {_difficulty}");
            }

            // SINGLETON PATTERN - Check cache first
            var cacheKey = $"{_languageSource.LanguageCode}:{_difficulty.Value}";
            
            if (_providerCache.TryGetValue(cacheKey, out var cachedProvider))
            {
                Console.WriteLine($"SINGLETON: Reusing cached TextProvider for {cacheKey}");
                return cachedProvider;
            }

            // Create new provider and cache it
            var newProvider = _providerRegistry[_difficulty.Value](_languageSource, _cacheService);
            _providerCache[cacheKey] = newProvider;
            
            Console.WriteLine($"SINGLETON: Created and cached new TextProvider for {cacheKey}");
            return newProvider;
        }

        public static Dictionary<string, string> GetSupportedLanguages()
        {
            return new Dictionary<string, string>
            {
                ["en"] = "English",
                ["sk"] = "Slovak",
                ["ar"] = "Arabic",
                ["ja"] = "Japanese"
            };
        }

        /// <summary>
        /// ✅ Get language name by code (static)
        /// </summary>
        public static string GetLanguageName(string languageCode)
        {
            var languages = GetSupportedLanguages();

            if (languages.TryGetValue(languageCode.ToLower(), out string? name))
            {
                return name;
            }

            return languageCode.ToUpper(); // Fallback to code itself
        }

        public IEnumerable<string> GetAvailableLanguageCodes()
        {
            return _languageRegistry.Keys;
        }

        public IEnumerable<DifficultyLevel> GetAvailableDifficulties()
        {
            return _providerRegistry.Keys;
        }
        
        /// <summary>
        /// SINGLETON - Get cached provider statistics
        /// </summary>
        public Dictionary<string, string> GetCachedProviders()
        {
            return _providerCache.Keys.ToDictionary(
                key => key, 
                key => $"{_providerCache[key].GetType().Name}"
            );
        }
        
        /// <summary>
        /// SINGLETON - Clear provider cache (for testing or memory management)
        /// </summary>
        public void ClearCache()
        {
            var count = _providerCache.Count;
            _providerCache.Clear();
            Console.WriteLine($"SINGLETON: Cleared {count} cached providers");
        }
        
        /// <summary>
        /// SINGLETON - Get cache statistics
        /// </summary>
        public int GetCachedProviderCount() => _providerCache.Count;
    }

}