using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Databases;
using Supabase;

namespace POT_SEM.Services.Patterns.Strategy.TextFetch
{
    public class DatabaseTextFetchStrategy : ITextFetchStrategy
    {
        private readonly Client _supabase;
        private readonly string _languageCode;

        public DatabaseTextFetchStrategy(Client supabase, string languageCode)
        {
            _supabase = supabase;
            _languageCode = languageCode.ToLower();
        }

        public string SourceName => $"Database ({_languageCode.ToUpper()})";

        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            try
            {
                var difficultyStr = criteria.Difficulty.ToString();
                var query = _supabase
                    .From<DatabaseText>()
                    .Where(x => x.LanguageCode == _languageCode)
                    .Where(x => x.Difficulty == difficultyStr);

                query = query.Limit(criteria.MaxResults ?? 10);
                
                var response = await query.Get();

                if (response?.Models == null || !response.Models.Any())
                {
                    return new List<Text>();
                }

                var texts = response.Models.Select(db => new Text
                {
                    Title = db.Title,
                    Content = db.Content,
                    Language = criteria.Language,
                    Difficulty = criteria.Difficulty,
                    Metadata = new TextMetadata
                    {
                        Source = SourceName,
                        EstimatedWordCount = db.WordCount,
                        Topics = string.IsNullOrEmpty(db.Topic) 
                            ? new List<string>() 
                            : new List<string> { db.Topic }
                    }
                }).ToList();

                return texts;
            }
            catch
            {
                return new List<Text>();
            }
        }

        public async Task<bool> SupportsTopicAsync(string topic)
        {
            try
            {
                var response = await _supabase
                    .From<DatabaseText>()
                    .Where(x => x.LanguageCode == _languageCode)
                    .Where(x => x.Title.Contains(topic))
                    .Limit(1)
                    .Get();

                return response.Models.Any();
            }
            catch
            {
                return false;
            }
        }
    }
}