using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY PATTERN
    /// Base handler for text fetching chain
    /// Each handler tries to fetch texts and passes remaining quota to next handler
    /// </summary>
    public abstract class TextFetchChainHandler
    {
        protected TextFetchChainHandler? _nextHandler;
        protected readonly ITopicGenerationStrategy _topicStrategy;

        protected TextFetchChainHandler(ITopicGenerationStrategy topicStrategy)
        {
            _topicStrategy = topicStrategy;
        }

        public TextFetchChainHandler SetNext(TextFetchChainHandler handler)
        {
            _nextHandler = handler;
            return handler;
        }

        public abstract Task<List<Text>> HandleAsync(TextSearchCriteria criteria, int remainingQuota);
    }

    /// <summary>
    /// Concrete handler for a single ITextFetchStrategy
    /// </summary>
    public class StrategyChainHandler : TextFetchChainHandler
    {
        private readonly ITextFetchStrategy _strategy;
        private readonly string _languageCode;

        public StrategyChainHandler(
            ITextFetchStrategy strategy, 
            string languageCode,
            ITopicGenerationStrategy topicStrategy) 
            : base(topicStrategy)
        {
            _strategy = strategy;
            _languageCode = languageCode;
        }

        public override async Task<List<Text>> HandleAsync(TextSearchCriteria criteria, int remainingQuota)
        {
            if (remainingQuota <= 0)
            {
                return new List<Text>();
            }

            var texts = new List<Text>();

            // Database strategy: Fetch directly
            if (_strategy.SourceName.Contains("Database"))
            {
                var fetchCriteria = new TextSearchCriteria
                {
                    Difficulty = criteria.Difficulty,
                    Language = criteria.Language,
                    Topic = criteria.Topic,
                    MinWordCount = criteria.MinWordCount,
                    MaxWordCount = criteria.MaxWordCount,
                    MaxResults = remainingQuota
                };
                texts = await _strategy.FetchTextsAsync(fetchCriteria);
            }
            // Other strategies: Generate topics first
            else
            {
                List<string> topics;
                
                if (!string.IsNullOrEmpty(criteria.Topic))
                {
                    topics = new List<string> { criteria.Topic };
                }
                else
                {
                    topics = await _topicStrategy.GenerateTopicsAsync(
                        _languageCode, 
                        criteria.Difficulty, 
                        remainingQuota);
                }

                if (topics.Any())
                {
                    texts = await FetchFromTopics(topics, criteria, remainingQuota);
                }
            }

            // If we got enough texts, return them
            if (texts.Count >= remainingQuota)
            {
                return texts.Take(remainingQuota).ToList();
            }

            // Otherwise, pass the remaining quota to the next handler
            if (_nextHandler != null)
            {
                var nextTexts = await _nextHandler.HandleAsync(criteria, remainingQuota - texts.Count);
                texts.AddRange(nextTexts);
            }

            return texts;
        }

        private async Task<List<Text>> FetchFromTopics(
            List<string> topics, 
            TextSearchCriteria criteria,
            int maxTexts)
        {
            var texts = new List<Text>();
            var fetchTasks = topics.Select(topic => FetchSingleTextSafe(topic, criteria));
            var results = await Task.WhenAll(fetchTasks);
            
            texts.AddRange(results.Where(t => t != null).Cast<Text>());
            return texts.Take(maxTexts).ToList();
        }

        private async Task<Text?> FetchSingleTextSafe(string topic, TextSearchCriteria criteria)
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

                var texts = await _strategy.FetchTextsAsync(topicCriteria);
                return texts.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
