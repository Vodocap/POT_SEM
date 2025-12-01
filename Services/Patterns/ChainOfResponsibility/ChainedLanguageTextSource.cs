using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.ChainOfResponsibility
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY PATTERN
    /// Language text source that chains multiple fetch strategies
    /// Each strategy tries to fulfill the request and passes remaining quota down the chain
    /// </summary>
    public class ChainedLanguageTextSource : ILanguageTextSource
    {
        private readonly string _languageCode;
        private readonly string _languageName;
        private readonly ITopicGenerationStrategy _topicStrategy;
        private readonly Dictionary<DifficultyLevel, TextFetchChainHandler>? _difficultyChains;
        private readonly TextFetchChainHandler? _defaultChain;

        public ChainedLanguageTextSource(
            string languageCode,
            string languageName,
            ITopicGenerationStrategy topicStrategy,
            Dictionary<DifficultyLevel, TextFetchChainHandler>? difficultyChains,
            TextFetchChainHandler? defaultChain)
        {
            _languageCode = languageCode;
            _languageName = languageName;
            _topicStrategy = topicStrategy;
            _difficultyChains = difficultyChains;
            _defaultChain = defaultChain;
        }

        public ChainedLanguageTextSource(
            string languageCode,
            string languageName,
            ITopicGenerationStrategy topicStrategy,
            TextFetchChainHandler chain)
            : this(languageCode, languageName, topicStrategy, null, chain)
        {
        }

        public string LanguageCode => _languageCode;
        public string LanguageName => _languageName;

        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var chain = GetChainForDifficulty(criteria.Difficulty);

            if (chain == null)
            {
                return new List<Text>();
            }

            var maxResults = criteria.MaxResults ?? 10;
            var texts = await chain.HandleAsync(criteria, maxResults);

            return texts.Take(maxResults).ToList();
        }

        private TextFetchChainHandler? GetChainForDifficulty(DifficultyLevel difficulty)
        {
            if (_difficultyChains != null && _difficultyChains.ContainsKey(difficulty))
            {
                return _difficultyChains[difficulty];
            }

            return _defaultChain;
        }

        public bool SupportsDifficulty(DifficultyLevel level)
        {
            if (_difficultyChains != null && _difficultyChains.ContainsKey(level))
            {
                return true;
            }

            return _defaultChain != null;
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
