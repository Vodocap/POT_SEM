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
                Console.WriteLine($"💾 {SourceName}: Querying {criteria.Difficulty}...");

                var difficultyStr = criteria.Difficulty.ToString();
                var query = _supabase
                    .From<DatabaseText>()
                    .Where(x => x.LanguageCode == _languageCode)
                    .Where(x => x.Difficulty == difficultyStr);

                query = query.Limit(criteria.MaxResults ?? 10);

                Console.WriteLine($"   🔍 Query: lang={_languageCode}, diff={criteria.Difficulty}, limit={criteria.MaxResults ?? 10}");
                
                var response = await query.Get();

                if (response?.Models == null || !response.Models.Any())
                {
                    Console.WriteLine($"   ⚠️ Database returned 0 results");
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

                Console.WriteLine($"   ✅ Got {texts.Count} texts:");
                foreach (var text in texts.Take(3))
                {
                    Console.WriteLine($"      • {text.Title} ({text.Metadata.EstimatedWordCount} words)");
                }
                if (texts.Count > 3)
                {
                    Console.WriteLine($"      ... and {texts.Count - 3} more");
                }

                return texts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {SourceName} error: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
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