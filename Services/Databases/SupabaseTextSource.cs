using Supabase;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Database
{
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
                var limit = criteria.MaxResults ?? 10;  // ✅ Použije MaxResults
                
                var query = _supabase
                    .From<DatabaseText>()
                    .Where(x => x.LanguageCode == LanguageCode)
                    .Where(x => x.Difficulty == criteria.Difficulty.ToString())
                    .Order("created_at", Postgrest.Constants.Ordering.Descending)
                    .Limit(limit);
                
                var response = await query.Get();
                
                if (response?.Models == null || !response.Models.Any())
                {
                    Console.WriteLine($"⚠️ No texts found in Supabase for {LanguageCode} - {criteria.Difficulty}");
                    return new List<Text>();
                }
                
                // ✅ OPRAVENÉ: Mapovanie DatabaseText → Text
                var texts = response.Models.Select(dbText => new Text
                {
                    Id = dbText.Id.ToString(),
                    Title = dbText.Title,
                    Content = dbText.Content,
                    Language = LanguageName,  // ✅ Meno jazyka (nie kód)
                    Difficulty = Enum.Parse<DifficultyLevel>(dbText.Difficulty),
                    Metadata = new TextMetadata
                    {
                        EstimatedWordCount = dbText.WordCount,
                        Topics = string.IsNullOrEmpty(dbText.Topic) 
                            ? new List<string>() 
                            : new List<string> { dbText.Topic },
                        Source = "Supabase",
                        SourceUrl = null
                    },
                    FetchedAt = dbText.CreatedAt
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
                    .Where(x => x.LanguageCode == LanguageCode)
                    .Select("topic")
                    .Get();
                
                return response?.Models
                    .Where(x => !string.IsNullOrEmpty(x.Topic))
                    .Select(x => x.Topic!)
                    .Distinct()
                    .OrderBy(x => x)
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