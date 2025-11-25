using Supabase;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Database
{
    /// <summary>
    /// Text source that fetches from Supabase database
    /// </summary>
    public class SupabaseTextSource : ILanguageTextSource
    {
        private readonly Client _supabase;
        
        public string LanguageCode { get; }
        public string LanguageName { get; }
        
        public SupabaseTextSource(Client supabase, string languageCode, string languageName)
        {
            _supabase = supabase;
            LanguageCode = languageCode;
            LanguageName = languageName;
        }
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            try
            {
                var query = _supabase
                    .From<DatabaseText>()
                    .Where(t => t.LanguageCode == LanguageCode)
                    .Where(t => t.Difficulty == criteria.Difficulty.ToString())
                    .Order(t => t.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(criteria.Count);
                
                var response = await query.Get();
                
                if (response?.Models == null || !response.Models.Any())
                {
                    Console.WriteLine($"⚠️ No texts found in Supabase for {LanguageCode} - {criteria.Difficulty}");
                    return new List<Text>();
                }
                
                var texts = response.Models.Select(dbText => new Text
                {
                    Title = dbText.Title,
                    Content = dbText.Content,
                    LanguageCode = dbText.LanguageCode,
                    Difficulty = Enum.Parse<DifficultyLevel>(dbText.Difficulty),
                    Topic = dbText.Topic,
                    WordCount = dbText.WordCount
                }).ToList();
                
                Console.WriteLine($"✅ Fetched {texts.Count} texts from Supabase ({LanguageCode} - {criteria.Difficulty})");
                return texts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Supabase fetch error: {ex.Message}");
                return new List<Text>();
            }
        }
        
        public bool SupportsDifficulty(DifficultyLevel level) => true;
        
        public async Task<List<string>> GetAvailableTopicsAsync()
        {
            try
            {
                var response = await _supabase
                    .From<DatabaseText>()
                    .Where(t => t.LanguageCode == LanguageCode)
                    .Select("topic")
                    .Get();
                
                return response?.Models
                    .Where(t => !string.IsNullOrEmpty(t.Topic))
                    .Select(t => t.Topic!)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to fetch topics: {ex.Message}");
                return new List<string>();
            }
        }
    }
}