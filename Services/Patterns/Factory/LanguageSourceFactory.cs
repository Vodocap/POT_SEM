using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.ChainOfResponsibility;
using POT_SEM.Services.Patterns.Strategy.TextFetch;
using POT_SEM.Services.Patterns.Decorator.TextSource;
using POT_SEM.Services.Databases;
using POT_SEM.Core.StrategyImplementations;
using Supabase;

namespace POT_SEM.Services.Patterns.Factory
{    public class LanguageSourceFactory
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
        /// Uses Chain of Responsibility pattern
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

            // Build difficulty-based chain map
            var chainMap = BuildChainMap(wiki, simpleWiki, gutenberg, config.LanguageCode, topicStrategy);

            // Add database as first priority for all levels
            if (_supabase != null)
            {
                var dbStrategy = new DatabaseTextFetchStrategy(_supabase, config.LanguageCode);
                AddDatabaseToChains(chainMap, dbStrategy, config.LanguageCode, topicStrategy);
            }

            // Build default chain
            TextFetchChainHandler defaultChain = new StrategyChainHandler(wiki, config.LanguageCode, topicStrategy);
            if (_supabase != null)
            {
                var dbHandler = new StrategyChainHandler(
                    new DatabaseTextFetchStrategy(_supabase, config.LanguageCode),
                    config.LanguageCode,
                    topicStrategy);
                dbHandler.SetNext(defaultChain);
                defaultChain = dbHandler;
            }

            // Create base chained source
            var baseSource = new ChainedLanguageTextSource(
                config.LanguageCode,
                config.LanguageName,
                topicStrategy,
                chainMap.Count > 0 ? chainMap : null,
                defaultChain
            );

            // Wrap with AutoSave decorator if storage service is available
            if (_storageService != null)
            {
                return new AutoSaveTextSourceWrapper(baseSource, _storageService);
            }

            return baseSource;
        }

        /// <summary>
        /// Builds chain map for different difficulty levels
        /// </summary>
        private Dictionary<DifficultyLevel, TextFetchChainHandler> BuildChainMap(
            ITextFetchStrategy wiki,
            ITextFetchStrategy? simpleWiki,
            ITextFetchStrategy? gutenberg,
            string languageCode,
            ITopicGenerationStrategy topicStrategy)
        {
            var map = new Dictionary<DifficultyLevel, TextFetchChainHandler>();

            // Beginner chain
            var beginnerWiki = new StrategyChainHandler(wiki, languageCode, topicStrategy);
            if (simpleWiki != null)
            {
                var simpleHandler = new StrategyChainHandler(simpleWiki, languageCode, topicStrategy);
                simpleHandler.SetNext(beginnerWiki);
                map[DifficultyLevel.Beginner] = simpleHandler;
            }
            else
            {
                map[DifficultyLevel.Beginner] = beginnerWiki;
            }

            // Intermediate chain
            map[DifficultyLevel.Intermediate] = new StrategyChainHandler(wiki, languageCode, topicStrategy);

            // Advanced chain
            var advancedWiki = new StrategyChainHandler(wiki, languageCode, topicStrategy);
            if (gutenberg != null)
            {
                var gutenbergHandler = new StrategyChainHandler(gutenberg, languageCode, topicStrategy);
                gutenbergHandler.SetNext(advancedWiki);
                map[DifficultyLevel.Advanced] = gutenbergHandler;
            }
            else
            {
                map[DifficultyLevel.Advanced] = advancedWiki;
            }

            return map;
        }

        /// <summary>
        /// Adds database strategy as first handler in all chains
        /// </summary>
        private void AddDatabaseToChains(
            Dictionary<DifficultyLevel, TextFetchChainHandler> chainMap,
            ITextFetchStrategy dbStrategy,
            string languageCode,
            ITopicGenerationStrategy topicStrategy)
        {
            foreach (var level in chainMap.Keys.ToList())
            {
                var existingChain = chainMap[level];
                var dbHandler = new StrategyChainHandler(dbStrategy, languageCode, topicStrategy);
                dbHandler.SetNext(existingChain);
                chainMap[level] = dbHandler;
            }
        }
    }
}