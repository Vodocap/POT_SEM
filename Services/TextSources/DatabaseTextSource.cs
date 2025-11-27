using Supabase;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.TextSources
{
    /// <summary>
    /// 💾 Database-first text source (Priority #1)
    /// </summary>
    public class DatabaseTextSource : ILanguageTextSource
    {
        private readonly Client _supabase;
        private readonly string _languageCode;
        private readonly string _languageName;
        
        public string LanguageCode => _languageCode;
        public string LanguageName => _languageName;
        
        public DatabaseTextSource(Client supabase, string languageCode, string languageName)
        {
            _supabase = supabase;
            _languageCode = languageCode;
            _languageName = languageName;
        }
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            try
            {
                Console.WriteLine($"💾 Database[{LanguageCode}]: Fetching {criteria.MaxResults ?? 10} texts for {criteria.Difficulty}...");
                
                var difficultyStr = criteria.Difficulty.ToString().ToLower();
                var langCode = _languageCode.ToLower();
                
                var query = _supabase
                    .From<Database.DatabaseText>()
                    .Select("*")
                    .Where(x => x.LanguageCode == langCode)
                    .Where(x => x.Difficulty == difficultyStr);
                
                // Topic filter (if specified)
                if (!string.IsNullOrEmpty(criteria.Topic))
                {
                    var topicLower = criteria.Topic.ToLower();
                    query = query.Where(x => x.Title.ToLower().Contains(topicLower) || 
                                            (x.Topic != null && x.Topic.ToLower().Contains(topicLower)));
                }
                
                // Word count filter
                if (criteria.MinWordCount > 0)
                {
                    query = query.Where(x => x.WordCount >= criteria.MinWordCount);
                }
                
                if (criteria.MaxWordCount > 0 && criteria.MaxWordCount < int.MaxValue)
                {
                    query = query.Where(x => x.WordCount <= criteria.MaxWordCount);
                }
                
                query = query.Limit(criteria.MaxResults ?? 10); // ✅ FIXED
                
                var response = await query.Get();
                
                var texts = response.Models.Select(record => new Text
                {
                    Title = record.Title,
                    Content = record.Content,
                    Language = record.LanguageCode,
                    Difficulty = Enum.Parse<DifficultyLevel>(record.Difficulty, true),
                    Metadata = new TextMetadata
                    {
                        Source = $"{LanguageName} (Database)",
                        EstimatedWordCount = record.WordCount,
                        SourceUrl = "",
                        Author = "Database",
                        Topics = string.IsNullOrEmpty(record.Topic) 
                            ? new List<string>() 
                            : new List<string> { record.Topic },
                        EstimatedReadingTimeMinutes = Math.Max(1, record.WordCount / 200)
                    }
                }).ToList();
                
                Console.WriteLine($"   ✅ Database returned {texts.Count} texts");
                
                return texts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Database error: {ex.Message}");
                return new List<Text>(); // Empty list = fallback to next source
            }
        }
        
        public bool SupportsDifficulty(DifficultyLevel level) => true;
        
        public async Task<List<string>> GetAvailableTopicsAsync()
        {
            try
            {
                var langCode = _languageCode.ToLower();
                
                var response = await _supabase
                    .From<Database.DatabaseText>()
                    .Select("title")
                    .Where(x => x.LanguageCode == langCode)
                    .Limit(30)
                    .Get();
                
                return response.Models
                    .Select(t => t.Title)
                    .Distinct()
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}