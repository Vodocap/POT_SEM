using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.ChainOfResponsibility.TextFetching;
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
            
            // Create database strategy if available
            var dbStrategy = _supabase != null 
                ? new DatabaseTextFetchStrategy(_supabase, config.LanguageCode)
                : null;

            // Build difficulty-based chain map (with database as first handler if available)
            var chainMap = BuildChainMap(wiki, simpleWiki, gutenberg, dbStrategy, config.LanguageCode, topicStrategy);

            // Build default chain
            TextFetchChainHandler defaultChain = new StrategyChainHandler(wiki, config.LanguageCode, topicStrategy);
            if (dbStrategy != null)
            {
                var dbHandler = new StrategyChainHandler(dbStrategy, config.LanguageCode, topicStrategy);
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
        /// Database is prepended as first handler if available
        /// </summary>
        private Dictionary<DifficultyLevel, TextFetchChainHandler> BuildChainMap(
            ITextFetchStrategy wiki,
            ITextFetchStrategy? simpleWiki,
            ITextFetchStrategy? gutenberg,
            ITextFetchStrategy? database,
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
                map[DifficultyLevel.Beginner] = PrependDatabase(simpleHandler, database, languageCode, topicStrategy);
            }
            else
            {
                map[DifficultyLevel.Beginner] = PrependDatabase(beginnerWiki, database, languageCode, topicStrategy);
            }

            // Intermediate chain
            var intermediateHandler = new StrategyChainHandler(wiki, languageCode, topicStrategy);
            map[DifficultyLevel.Intermediate] = PrependDatabase(intermediateHandler, database, languageCode, topicStrategy);

            // Advanced chain
            var advancedWiki = new StrategyChainHandler(wiki, languageCode, topicStrategy);
            if (gutenberg != null)
            {
                var gutenbergHandler = new StrategyChainHandler(gutenberg, languageCode, topicStrategy);
                gutenbergHandler.SetNext(advancedWiki);
                map[DifficultyLevel.Advanced] = PrependDatabase(gutenbergHandler, database, languageCode, topicStrategy);
            }
            else
            {
                map[DifficultyLevel.Advanced] = PrependDatabase(advancedWiki, database, languageCode, topicStrategy);
            }

            return map;
        }

        /// <summary>
        /// Helper to prepend database handler to an existing chain
        /// </summary>
        private TextFetchChainHandler PrependDatabase(
            TextFetchChainHandler existingChain,
            ITextFetchStrategy? database,
            string languageCode,
            ITopicGenerationStrategy topicStrategy)
        {
            if (database == null)
            {
                return existingChain;
            }

            var dbHandler = new StrategyChainHandler(database, languageCode, topicStrategy);
            dbHandler.SetNext(existingChain);
            return dbHandler;
        }
    }
}