using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.FetchStrategies;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;  

namespace POT_SEM.Services.TextSources
{
    /// <summary>
    /// BRIDGE IMPLEMENTATION - English texts
    /// Používa STRATEGY pattern pre výber zdrojov
    /// </summary>
    public class EnglishTextSource : ILanguageTextSource
    {
        private readonly Dictionary<DifficultyLevel, ITextFetchStrategy> _strategies;
        
        public EnglishTextSource(HttpClient httpClient)
        {
            // STRATEGY - mapovanie difficulty → zdroj
            _strategies = new Dictionary<DifficultyLevel, ITextFetchStrategy>
            {
                [DifficultyLevel.Beginner] = new SimpleWikipediaStrategy(httpClient),
                [DifficultyLevel.Intermediate] = new WikipediaStrategy(httpClient),
                [DifficultyLevel.Advanced] = new GutenbergStrategy(httpClient)
            };
        }
        
        public string LanguageCode => "en";
        public string LanguageName => "English";
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            if (_strategies.TryGetValue(criteria.Difficulty, out var strategy))
            {
                Console.WriteLine($"📚 {LanguageName}: Using {strategy.SourceName} for {criteria.Difficulty}");
                return await strategy.FetchTextsAsync(criteria);
            }
            
            return new List<Text>();
        }
        
        public bool SupportsDifficulty(DifficultyLevel level)
        {
            return _strategies.ContainsKey(level);
        }
        
        public async Task<List<string>> GetAvailableTopicsAsync()
        {
            return new List<string>
            {
                "Science", "History", "Technology", "Culture", 
                "Sports", "Music", "Travel", "Food"
            };
        }
    }
}