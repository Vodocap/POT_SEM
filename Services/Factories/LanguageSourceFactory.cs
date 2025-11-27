using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.TextSources;
using POT_SEM.Services.TextFetchStrategies;
using Supabase;

namespace POT_SEM.Services.Factories
{
    /// <summary>
    /// FACTORY + TEMPLATE METHOD PATTERN
    /// Eliminates code duplication by using a configuration-based approach
    /// </summary>
    public class LanguageSourceFactory
    {
        private readonly HttpClient _httpClient;
        private readonly Client? _supabase;

        public LanguageSourceFactory(HttpClient httpClient, Client? supabase = null)
        {
            _httpClient = httpClient;
            _supabase = supabase;
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

            return new CompositeLanguageTextSource(
                config.LanguageCode,
                config.LanguageName,
                topicStrategy,
                strategyMap.Count > 0 ? strategyMap : null,
                defaultStrategies
            );
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