using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.TextSources
{
    /// <summary>
    /// Composite language text source with difficulty-aware strategy selection
    /// </summary>
    public class CompositeLanguageTextSource : ILanguageTextSource
    {
        private readonly string _languageCode;
        private readonly string _languageName;
        private readonly ITopicGenerationStrategy _topicStrategy;
        private readonly Dictionary<DifficultyLevel, List<ITextFetchStrategy>>? _difficultyStrategies;
        private readonly List<ITextFetchStrategy> _defaultStrategies;

        /// <summary>
        /// ✅ Full constructor - 5 parameters (for factory with difficulty-based strategies)
        /// </summary>
        public CompositeLanguageTextSource(
            string languageCode,
            string languageName,
            ITopicGenerationStrategy topicStrategy,
            Dictionary<DifficultyLevel, List<ITextFetchStrategy>>? difficultyStrategies,
            List<ITextFetchStrategy> defaultStrategies)
        {
            _languageCode = languageCode;
            _languageName = languageName;
            _topicStrategy = topicStrategy;
            _difficultyStrategies = difficultyStrategies;
            _defaultStrategies = defaultStrategies ?? new List<ITextFetchStrategy>();
        }

        /// <summary>
        /// ✅ Simple constructor - 4 parameters (backward compatible)
        /// </summary>
        public CompositeLanguageTextSource(
            string languageCode,
            string languageName,
            ITopicGenerationStrategy topicStrategy,
            List<ITextFetchStrategy> fetchStrategies)
            : this(languageCode, languageName, topicStrategy, null, fetchStrategies)
        {
        }

        public string LanguageCode => _languageCode;
        public string LanguageName => _languageName;

        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            Console.WriteLine($"📚 [{LanguageName}] Fetching {criteria.MaxResults ?? 10} texts ({criteria.Difficulty})...");

            var strategies = GetStrategiesForDifficulty(criteria.Difficulty);

            if (!strategies.Any())
            {
                Console.WriteLine($"⚠️ No strategies available for {criteria.Difficulty}");
                return new List<Text>();
            }

            var allTexts = new List<Text>();

            foreach (var strategy in strategies)
            {
                if (allTexts.Count >= (criteria.MaxResults ?? 10))
                {
                    break;
                }

                Console.WriteLine($"🔍 Trying {strategy.SourceName}...");

                List<Text> strategyTexts;

                // Db: Fetch directly WITHOUT topics
                if (strategy.SourceName.Contains("Database"))
                {
                    Console.WriteLine($"   💾 Fetching directly from database (no topics needed)");
                    strategyTexts = await strategy.FetchTextsAsync(criteria);
                }
                // Other: Generate topics first
                else
                {
                    List<string> topics;
                    
                    if (!string.IsNullOrEmpty(criteria.Topic))
                    {
                        topics = new List<string> { criteria.Topic };
                    }
                    else
                    {
                        Console.WriteLine($"   🎲 Generating topics for {strategy.SourceName}...");
                        topics = await _topicStrategy.GenerateTopicsAsync(
                            LanguageCode, 
                            criteria.Difficulty, 
                            criteria.MaxResults ?? 10);
                    }

                    if (!topics.Any())
                    {
                        Console.WriteLine($"   ⚠️ No topics generated, skipping {strategy.SourceName}");
                        continue;
                    }

                    strategyTexts = await FetchFromStrategy(strategy, topics, criteria);
                }

                if (strategyTexts.Any())
                {
                    allTexts.AddRange(strategyTexts);
                    Console.WriteLine($"   ✅ Got {strategyTexts.Count} texts from {strategy.SourceName}");
                    
                    // ✅ If we got enough texts, stop trying other strategies
                    if (allTexts.Count >= (criteria.MaxResults ?? 10))
                    {
                        Console.WriteLine($"   ℹ️ Got enough texts ({allTexts.Count}), stopping");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"   ⚠️ {strategy.SourceName} returned 0 texts, trying next...");
                }
            }

            Console.WriteLine($"✅ Total: {allTexts.Count} texts from [{LanguageName}]");
            
            return allTexts.Take(criteria.MaxResults ?? 10).ToList();
        }

        /// <summary>
        /// ✅ Get strategies for specific difficulty level
        /// Falls back to default strategies if no difficulty-specific strategies exist
        /// </summary>
        private List<ITextFetchStrategy> GetStrategiesForDifficulty(DifficultyLevel difficulty)
        {
            // If we have difficulty-specific strategies, use them
            if (_difficultyStrategies != null && _difficultyStrategies.ContainsKey(difficulty))
            {
                var strategies = _difficultyStrategies[difficulty];
                Console.WriteLine($"   Using {strategies.Count} difficulty-specific strategies for {difficulty}");
                foreach (var s in strategies)
                {
                    Console.WriteLine($"      • {s.SourceName}");
                }
                return strategies;
            }

            // Fallback to default strategies
            Console.WriteLine($"   Using {_defaultStrategies.Count} default strategies");
            return _defaultStrategies;
        }

        private async Task<List<Text>> FetchFromStrategy(
            ITextFetchStrategy strategy, 
            List<string> topics, 
            TextSearchCriteria criteria)
        {
            var texts = new List<Text>();

            var fetchTasks = topics.Select(topic => FetchSingleTextSafe(strategy, topic, criteria));

            var results = await Task.WhenAll(fetchTasks);

            texts.AddRange(results.Where(t => t != null).Cast<Text>());

            return texts;
        }

        private async Task<Text?> FetchSingleTextSafe(
            ITextFetchStrategy strategy, 
            string topic, 
            TextSearchCriteria criteria)
        {
            try
            {
                var topicCriteria = new TextSearchCriteria
                {
                    Difficulty = criteria.Difficulty,
                    Language = criteria.Language,
                    Topic = topic,
                    MinWordCount = criteria.MinWordCount,
                    MaxWordCount = criteria.MaxWordCount,
                    MaxResults = 1
                };

                var texts = await strategy.FetchTextsAsync(topicCriteria);
                return texts.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ⚠️ {topic}: {ex.Message}");
                return null;
            }
        }

        public bool SupportsDifficulty(DifficultyLevel level)
        {
            // If we have specific strategies for this level, we support it
            if (_difficultyStrategies != null && _difficultyStrategies.ContainsKey(level))
            {
                return true;
            }

            // If we have default strategies, we support all levels
            return _defaultStrategies.Any();
        }

        public async Task<List<string>> GetAvailableTopicsAsync()
        {
            return await _topicStrategy.GenerateTopicsAsync(
                LanguageCode, 
                DifficultyLevel.Intermediate, 
                20);
        }
    }
}