using Supabase;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Databases
{
    public class TextStorageService
    {
        private readonly Client _supabase;
        private readonly HashSet<string> _processedKeys = new();
        
        public TextStorageService(Client supabase)
        {
            _supabase = supabase;
        }
        
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
                    _processedKeys.Add(key);
                    return false;
                }
                
                var dbText = new DatabaseText
                {
                    LanguageCode = languageCode,
                    Difficulty = text.Difficulty.ToString(),
                    Title = text.Title,
                    Content = text.Content,
                    Topic = text.Metadata.Topics.FirstOrDefault(),
                    WordCount = text.Metadata.EstimatedWordCount,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow  
                };
                
                var response = await _supabase
                    .From<DatabaseText>()
                    .Insert(dbText);
                
                // ✅ Check response
                if (response?.Models?.Any() != true)
                {
                    return false;
                }
                
                _processedKeys.Add(key);
                return true;
            }
            catch
            {
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
            catch
            {
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