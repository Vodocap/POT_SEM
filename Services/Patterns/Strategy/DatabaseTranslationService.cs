using POT_SEM.Core.Interfaces;
using POT_SEM.Services.Databases;
using Supabase;

namespace POT_SEM.Services.Patterns.Strategy
{
    /// <summary>
    /// Database-based translation lookup
    /// Uses Supabase for persistent translation cache
    /// </summary>
    public class DatabaseTranslationService : ITranslationStrategy
    {
        private readonly Client _supabase;
        
        public string StrategyName => "Supabase Database Cache";
        
        public DatabaseTranslationService(Client supabase)
        {
            _supabase = supabase;
        }
        
        public async Task<string?> TranslateWordAsync(string word, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return null;
            }
            
            try
            {
                var normalized = word.ToLower().Trim();
                
                var result = await _supabase
                    .From<DatabaseWordTranslation>()
                    .Where(t => t.SourceLang == sourceLang.ToLower())
                    .Where(t => t.TargetLang == targetLang.ToLower())
                    .Where(t => t.OriginalWord == normalized)
                    .Single();
                
                if (result != null)
                {
                    _ = IncrementUsageAsync(result.Id);
                    
                    return result.Translation;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        public async Task<string?> TranslateSentenceAsync(string sentence, string sourceLang, string targetLang)
        {
            return await Task.FromResult<string?>(null);
        }
        
        public async Task<Dictionary<string, string>> TranslateBatchAsync(
            IEnumerable<string> words, 
            string sourceLang, 
            string targetLang)
        {
            var results = new Dictionary<string, string>();
            
            try
            {
                var normalizedWords = words.Select(w => w.ToLower().Trim()).ToList();
                
                var dbResults = await _supabase
                    .From<DatabaseWordTranslation>()
                    .Where(t => t.SourceLang == sourceLang.ToLower())
                    .Where(t => t.TargetLang == targetLang.ToLower())
                    .Filter("original_word", Postgrest.Constants.Operator.In, normalizedWords)
                    .Get();
                
                if (dbResults?.Models != null)
                {
                    foreach (var translation in dbResults.Models)
                    {
                        results[translation.OriginalWord] = translation.Translation;
                        
                        _ = IncrementUsageAsync(translation.Id);
                    }
                }
            }
            catch
            {
            }
            
            return results;
        }
        
        public async Task SaveTranslationAsync(
            string originalWord, 
            string translation, 
            string sourceLang, 
            string targetLang,
            string? transliteration = null,
            string? furigana = null)
        {
            try
            {
                var normalized = originalWord.ToLower().Trim();
                
                var newTranslation = new DatabaseWordTranslation
                {
                    SourceLang = sourceLang.ToLower(),
                    TargetLang = targetLang.ToLower(),
                    OriginalWord = normalized,
                    Translation = translation,
                    Transliteration = transliteration,
                    Furigana = furigana,
                    UsageCount = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                await _supabase
                    .From<DatabaseWordTranslation>()
                    .Insert(newTranslation);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("duplicate key"))
                {
                }
            }
        }
        
        private async Task IncrementUsageAsync(long translationId)
        {
            try
            {
                var translation = await _supabase
                    .From<DatabaseWordTranslation>()
                    .Where(t => t.Id == translationId)
                    .Single();
                
                if (translation != null)
                {
                    translation.UsageCount++;
                    translation.UpdatedAt = DateTime.UtcNow;
                    
                    await translation.Update<DatabaseWordTranslation>();
                }
            }
            catch
            {
            }
        }
    }
}