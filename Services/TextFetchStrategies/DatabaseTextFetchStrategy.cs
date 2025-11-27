using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Database;
using Supabase;

namespace POT_SEM.Services.TextFetchStrategies
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
                var query = _supabase
                    .From<DatabaseText>()
                    .Where(x => x.LanguageCode == _languageCode)
                    .Where(x => x.Difficulty == criteria.Difficulty.ToString());

                if (!string.IsNullOrEmpty(criteria.Topic))
                {
                    query = query.Where(x => x.Title.Contains(criteria.Topic));
                }

                if (criteria.MinWordCount > 0)
                {
                    query = query.Where(x => x.WordCount >= criteria.MinWordCount);
                }

                if (criteria.MaxWordCount < int.MaxValue)
                {
                    query = query.Where(x => x.WordCount <= criteria.MaxWordCount);
                }

                query = query.Limit(criteria.MaxResults ?? 10);

                var response = await query.Get();

                return response.Models.Select(db => new Text
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {SourceName} error: {ex.Message}");
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