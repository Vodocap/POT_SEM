using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.TextSources;
using POT_SEM.Services.TextFetchStrategies;
using POT_SEM.Services.Decorators;
using POT_SEM.Services.Database;
using Supabase;

namespace POT_SEM.Services.Factories
{
    /// <summary>
    /// FACTORY + TEMPLATE METHOD PATTERN
    /// Eliminates code duplication by using a configuration-based approach
    /// Now wraps sources with AutoSave decorator when database is available
    /// </summary>
    public class LanguageSourceFactory
    {
        private readonly HttpClient _httpClient;
        private readonly Client? _supabase;
        private readonly TextStorageService? _storageService;

        public LanguageSourceFactory(
            HttpClient httpClient, 
            Client? supabase = null,
            TextStorageService? storageService = null)
        {
            _httpClient = httpClient;
            _supabase = supabase;
            _storageService = storageService;
        }

        /// <summary>
        /// Language source configuration
        /// </summary>
        private class LanguageConfig
        {
            public required string LanguageCode { get; init; }
            public required string LanguageName { get; init; }
            public bool HasSimpleWikipedia { get; init; }
            public bool HasGutenberg { get; init; }
        }

        public ILanguageTextSource CreateEnglishSource(ITopicGenerationStrategy topicStrategy)
        {
            return CreateLanguageSource(
                new LanguageConfig
                {
                    LanguageCode = "en",
                    LanguageName = "English",
                    HasSimpleWikipedia = true,
                    HasGutenberg = true
                },
                topicStrategy
            );
        }

        public ILanguageTextSource CreateSlovakSource(ITopicGenerationStrategy topicStrategy)
        {
            return CreateLanguageSource(
                new LanguageConfig
                {
                    LanguageCode = "sk",
                    LanguageName = "Slovak",
                    HasSimpleWikipedia = false,
                    HasGutenberg = false
                },
                topicStrategy
            );
        }

        public ILanguageTextSource CreateArabicSource(ITopicGenerationStrategy topicStrategy)
        {
            return CreateLanguageSource(
                new LanguageConfig
                {
                    LanguageCode = "ar",
                    LanguageName = "Arabic",
                    HasSimpleWikipedia = false,
                    HasGutenberg = true
                },
                topicStrategy
            );
        }

        public ILanguageTextSource CreateJapaneseSource(ITopicGenerationStrategy topicStrategy)
        {
            return CreateLanguageSource(
                new LanguageConfig
                {
                    LanguageCode = "ja",
                    LanguageName = "Japanese",
                    HasSimpleWikipedia = false,
                    HasGutenberg = true
                },
                topicStrategy
            );
        }

        /// <summary>
        /// TEMPLATE METHOD - Creates language source based on configuration
        /// ✅ Now wraps result with AutoSave decorator
        /// </summary>
        private ILanguageTextSource CreateLanguageSource(
            LanguageConfig config,
            ITopicGenerationStrategy topicStrategy)
        {
            // Create base strategies
            var wiki = new WikipediaStrategy(_httpClient, config.LanguageCode);
            var simpleWiki = config.HasSimpleWikipedia 
                ? new SimpleWikipediaStrategy(_httpClient) 
                : null;
            var gutenberg = config.HasGutenberg 
                ? new GutenbergStrategy(_httpClient) 
                : null;

            // Build difficulty-based strategy map
            var strategyMap = BuildStrategyMap(wiki, simpleWiki, gutenberg);

            // Add database as first priority for all levels
            if (_supabase != null)
            {
                var dbStrategy = new DatabaseTextFetchStrategy(_supabase, config.LanguageCode);
                AddDatabaseToPriority(strategyMap, dbStrategy);
            }

            // Build default strategies
            var defaultStrategies = new List<ITextFetchStrategy> { wiki };
            if (_supabase != null)
            {
                defaultStrategies.Insert(0, new DatabaseTextFetchStrategy(_supabase, config.LanguageCode));
            }

            // ✅ Create base composite source
            var baseSource = new CompositeLanguageTextSource(
                config.LanguageCode,
                config.LanguageName,
                topicStrategy,
                strategyMap.Count > 0 ? strategyMap : null,
                defaultStrategies
            );

            // ✅ Wrap with AutoSave decorator if storage service is available
            if (_storageService != null)
            {
                return new AutoSaveTextSourceWrapper(baseSource, _storageService);
            }

            return baseSource;
        }

        /// <summary>
        /// Builds strategy map for different difficulty levels
        /// </summary>
        private Dictionary<DifficultyLevel, List<ITextFetchStrategy>> BuildStrategyMap(
            ITextFetchStrategy wiki,
            ITextFetchStrategy? simpleWiki,
            ITextFetchStrategy? gutenberg)
        {
            var map = new Dictionary<DifficultyLevel, List<ITextFetchStrategy>>();

            // Beginner
            if (simpleWiki != null)
            {
                map[DifficultyLevel.Beginner] = new List<ITextFetchStrategy>
                {
                    simpleWiki,
                    wiki
                };
            }
            else
            {
                map[DifficultyLevel.Beginner] = new List<ITextFetchStrategy> { wiki };
            }

            // Intermediate
            map[DifficultyLevel.Intermediate] = new List<ITextFetchStrategy> { wiki };

            // Advanced
            if (gutenberg != null)
            {
                map[DifficultyLevel.Advanced] = new List<ITextFetchStrategy>
                {
                    gutenberg,  // Classic literature first
                    wiki        // Fallback to Wikipedia
                };
            }
            else
            {
                map[DifficultyLevel.Advanced] = new List<ITextFetchStrategy> { wiki };
            }

            return map;
        }

        /// <summary>
        /// Adds database strategy as first priority for all difficulty levels
        /// </summary>
        private void AddDatabaseToPriority(
            Dictionary<DifficultyLevel, List<ITextFetchStrategy>> strategyMap,
            ITextFetchStrategy dbStrategy)
        {
            foreach (var level in strategyMap.Keys.ToList())
            {
                strategyMap[level].Insert(0, dbStrategy);
            }
        }
    }
}