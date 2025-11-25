using Supabase;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Database
{
    public class TextStorageService
    {
        private readonly Client _supabase;
        private readonly HashSet<string> _processedKeys = new();
        
        public TextStorageService(Client supabase)
        {
            _supabase = supabase;
        }
        
        // ✅ OPRAVENÉ: Použije text.Language ako languageCode
        public async Task<bool> SaveTextAsync(Text text, string languageCode)
        {
            try
            {
                var key = $"{languageCode}_{text.Title}";
                
                if (_processedKeys.Contains(key))
                {
                    return false;
                }
                
                // Check if exists
                var existing = await _supabase
                    .From<DatabaseText>()
                    .Where(x => x.Title == text.Title)
                    .Where(x => x.LanguageCode == languageCode)
                    .Limit(1)
                    .Get();
                
                if (existing?.Models?.Any() == true)
                {
                    Console.WriteLine($"⏭️ Skipped duplicate: {text.Title}");
                    _processedKeys.Add(key);
                    return false;
                }
                
                // ✅ Mapovanie Text → DatabaseText
                var dbText = new DatabaseText
                {
                    LanguageCode = languageCode,
                    Difficulty = text.Difficulty.ToString(),
                    Title = text.Title,
                    Content = text.Content,
                    Topic = text.Metadata.Topics.FirstOrDefault(),  // ✅ Prvý topic
                    WordCount = text.Metadata.EstimatedWordCount    // ✅ Z Metadata
                };
                
                await _supabase.From<DatabaseText>().Insert(dbText);
                
                _processedKeys.Add(key);
                Console.WriteLine($"💾 Saved to Supabase: {text.Title} ({languageCode})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Save failed for '{text.Title}': {ex.Message}");
                return false;
            }
        }
        
        public async Task<int> SaveTextsAsync(List<Text> texts, string languageCode)
        {
            if (!texts.Any())
            {
                return 0;
            }
            
            int savedCount = 0;
            
            foreach (var text in texts)
            {
                if (await SaveTextAsync(text, languageCode))
                {
                    savedCount++;
                }
                
                await Task.Delay(50);
            }
            
            if (savedCount > 0)
            {
                Console.WriteLine($"✅ Batch save: {savedCount}/{texts.Count} new texts saved");
            }
            
            return savedCount;
        }
        
        public async Task<DatabaseStats> GetStatsAsync()
        {
            try
            {
                var response = await _supabase
                    .From<DatabaseText>()
                    .Get();
                
                var texts = response?.Models?.ToList() ?? new();
                
                return new DatabaseStats
                {
                    TotalTexts = texts.Count,
                    LanguageBreakdown = texts
                        .GroupBy(t => t.LanguageCode)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    DifficultyBreakdown = texts
                        .GroupBy(t => t.Difficulty)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    UniqueTopics = texts
                        .Where(t => !string.IsNullOrEmpty(t.Topic))
                        .Select(t => t.Topic!)
                        .Distinct()
                        .Count(),
                    OldestText = texts.OrderBy(t => t.CreatedAt).FirstOrDefault()?.CreatedAt,
                    NewestText = texts.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.CreatedAt
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Stats fetch failed: {ex.Message}");
                return new DatabaseStats();
            }
        }
    }
    
    public class DatabaseStats
    {
        public int TotalTexts { get; set; }
        public Dictionary<string, int> LanguageBreakdown { get; set; } = new();
        public Dictionary<string, int> DifficultyBreakdown { get; set; } = new();
        public int UniqueTopics { get; set; }
        public DateTime? OldestText { get; set; }
        public DateTime? NewestText { get; set; }
    }
}