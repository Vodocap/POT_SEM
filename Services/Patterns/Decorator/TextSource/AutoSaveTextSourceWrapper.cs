using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Databases;

namespace POT_SEM.Services.Patterns.Decorator.TextSource
{
    /// <summary>
    /// DECORATOR PATTERN
    /// Wraps around language text source and saves fetched texts to database in background
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
            var texts = await _innerSource.FetchTextsAsync(criteria);

            if (texts.Any())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var savedCount = await _storageService.SaveTextsAsync(texts, LanguageCode);
                    }
                    catch
                    {
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