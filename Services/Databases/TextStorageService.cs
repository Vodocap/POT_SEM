using Supabase;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Database
{
    /// <summary>
    /// Service for storing texts to Supabase (auto-save from Wikipedia, etc.)
    /// </summary>
    public class TextStorageService
    {
        private readonly Client _supabase;
        private readonly HashSet<string> _processedKeys = new(); // Prevent duplicate saves in same session
        
        public TextStorageService(Client supabase)
        {
            _supabase = supabase;
        }
        
        /// <summary>
        /// Save single text to database (prevents duplicates)
        /// </summary>
        public async Task<bool> SaveTextAsync(Text text)
        {
            try
            {
                // Create unique key for this text
                var key = $"{text.LanguageCode}_{text.Title}";
                
                // Skip if already processed in this session
                if (_processedKeys.Contains(key))
                {
                    return false;
                }
                
                // Check if exists in database
                var existing = await _supabase
                    .From<DatabaseText>()
                    .Where(t => t.Title == text.Title)
                    .Where(t => t.LanguageCode == text.LanguageCode)
                    .Limit(1)
                    .Get();
                
                if (existing?.Models?.Any() == true)
                {
                    Console.WriteLine($"⏭️ Skipped duplicate: {text.Title}");
                    _processedKeys.Add(key);
                    return false;
                }
                
                // Insert new text
                var dbText = new DatabaseText
                {
                    LanguageCode = text.LanguageCode,
                    Difficulty = text.Difficulty.ToString(),
                    Title = text.Title,
                    Content = text.Content,
                    Topic = text.Topic,
                    WordCount = text.WordCount
                };
                
                await _supabase.From<DatabaseText>().Insert(dbText);
                
                _processedKeys.Add(key);
                Console.WriteLine($"💾 Saved to Supabase: {text.Title} ({text.LanguageCode})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Save failed for '{text.Title}': {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Save multiple texts in batch
        /// </summary>
        public async Task<int> SaveTextsAsync(List<Text> texts)
        {
            if (!texts.Any())
            {
                return 0;
            }
            
            int savedCount = 0;
            
            foreach (var text in texts)
            {
                if (await SaveTextAsync(text))
                {
                    savedCount++;
                }
                
                // Small delay to avoid rate limiting
                await Task.Delay(50);
            }
            
            if (savedCount > 0)
            {
                Console.WriteLine($"✅ Batch save: {savedCount}/{texts.Count} new texts saved");
            }
            
            return savedCount;
        }
        
        /// <summary>
        /// Get database statistics
        /// </summary>
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