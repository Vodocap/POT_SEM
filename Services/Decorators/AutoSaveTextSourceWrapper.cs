using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Database;

namespace POT_SEM.Services.Decorators
{
    /// <summary>
    /// DECORATOR PATTERN
    /// Automatically saves fetched texts to database in background
    /// </summary>
    public class AutoSaveTextSourceWrapper : ILanguageTextSource
    {
        private readonly ILanguageTextSource _innerSource;
        private readonly TextStorageService _storageService;

        public AutoSaveTextSourceWrapper(
            ILanguageTextSource innerSource,
            TextStorageService storageService)
        {
            _innerSource = innerSource;
            _storageService = storageService;
        }

        public string LanguageCode => _innerSource.LanguageCode;
        public string LanguageName => _innerSource.LanguageName;

        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            // ✅ Fetch texts from inner source
            var texts = await _innerSource.FetchTextsAsync(criteria);

            // ✅ Save to database in background (non-blocking)
            if (texts.Any())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var savedCount = await _storageService.SaveTextsAsync(texts, LanguageCode);
                        
                        if (savedCount > 0)
                        {
                            Console.WriteLine($"💾 Auto-saved {savedCount} texts to database ({LanguageCode})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Auto-save failed: {ex.Message}");
                    }
                });
            }

            return texts;
        }

        public bool SupportsDifficulty(DifficultyLevel difficulty)
        {
            return _innerSource.SupportsDifficulty(difficulty);
        }

        public Task<List<string>> GetAvailableTopicsAsync()
        {
            return _innerSource.GetAvailableTopicsAsync();
        }
    }
}