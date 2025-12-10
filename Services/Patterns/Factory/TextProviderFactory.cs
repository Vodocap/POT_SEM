using POT_SEM.Services.TextProviders;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Linq;

namespace POT_SEM.Services.Patterns.Factory
{
    /// <summary>
    /// Factory creating TextProvider instances
    /// </summary>
    public class TextProviderFactory
    {
        private readonly LanguageSourceFactory _languageFactory;
        private readonly ITopicGenerationStrategy _topicStrategy;
        private readonly ITextCacheService? _cacheService;

        private DifficultyLevel? _difficulty;
        private ILanguageTextSource? _languageSource;

        // Language factory registry
        private readonly Dictionary<string, Func<ITopicGenerationStrategy, ILanguageTextSource>> _languageRegistry;

        // Provider factory registry
        private readonly Dictionary<DifficultyLevel, Func<ILanguageTextSource, ITextCacheService?, TextProvider>> _providerRegistry;
        
        private readonly Dictionary<string, TextProvider> _providerCache = new();

        public TextProviderFactory(
            LanguageSourceFactory languageFactory,
            ITopicGenerationStrategy topicStrategy,
            ITextCacheService? cacheService = null)
        {
            _languageFactory = languageFactory;
            _topicStrategy = topicStrategy;
            _cacheService = cacheService;

            _languageRegistry = new Dictionary<string, Func<ITopicGenerationStrategy, ILanguageTextSource>>
            {
                ["en"] = _languageFactory.CreateEnglishSource,
                ["sk"] = _languageFactory.CreateSlovakSource,
                ["ar"] = _languageFactory.CreateArabicSource,
                ["ja"] = _languageFactory.CreateJapaneseSource
            };

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

            var cacheKey = $"{_languageSource.LanguageCode}:{_difficulty.Value}";
            
            if (_providerCache.TryGetValue(cacheKey, out var cachedProvider))
            {
                Console.WriteLine($"SINGLETON: Reusing cached TextProvider for {cacheKey}");
                return cachedProvider;
            }

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
        /// Get language name by code
        /// </summary>
        public static string GetLanguageName(string languageCode)
        {
            var languages = GetSupportedLanguages();

            if (languages.TryGetValue(languageCode.ToLower(), out string? name))
            {
                return name;
            }

            return languageCode.ToUpper(); 
        }

    }

}