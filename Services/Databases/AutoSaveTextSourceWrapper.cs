using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Database
{
    /// <summary>
    /// Wrapper that auto-saves fetched texts to Supabase database
    /// </summary>
    public class AutoSaveTextSourceWrapper : ILanguageTextSource
    {
        private readonly ILanguageTextSource _innerSource;
        private readonly TextStorageService _storage;
        
        public string LanguageCode => _innerSource.LanguageCode;
        public string LanguageName => $"{_innerSource.LanguageName} + Auto-Save";
        
        public AutoSaveTextSourceWrapper(
            ILanguageTextSource innerSource,
            TextStorageService storage)
        {
            _innerSource = innerSource;
            _storage = storage;
        }
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            // Fetch from original source (Wikipedia, etc.)
            var texts = await _innerSource.FetchTextsAsync(criteria);
            
            // Save to database in background (fire and forget)
            if (texts.Any())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _storage.SaveTextsAsync(texts);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Background save failed: {ex.Message}");
                    }
                });
            }
            
            return texts;
        }
        
        public bool SupportsDifficulty(DifficultyLevel level) 
            => _innerSource.SupportsDifficulty(level);
        
        public Task<List<string>> GetAvailableTopicsAsync() 
            => _innerSource.GetAvailableTopicsAsync();
    }
}