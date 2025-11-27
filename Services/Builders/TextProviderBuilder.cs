using POT_SEM.Core.BridgeAbstractions;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Factories;

namespace POT_SEM.Services.Builders
{
    /// <summary>
    /// BUILDER + FLUENT API PATTERN
    /// Uses Registry Pattern to eliminate switch statements
    /// </summary>
    public class TextProviderBuilder
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

        public TextProviderBuilder(
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

        public TextProviderBuilder ForLanguage(string languageCode)
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

        public TextProviderBuilder ForCustomSource(ILanguageTextSource source)
        {
            _languageSource = source;
            return this;
        }

        public TextProviderBuilder ForDifficulty(DifficultyLevel difficulty)
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

            return _providerRegistry[_difficulty.Value](_languageSource, _cacheService);
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
    }

}