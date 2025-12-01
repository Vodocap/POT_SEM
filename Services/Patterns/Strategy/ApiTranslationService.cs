using POT_SEM.Core.Interfaces;
using System.Text.Json;
using System.Web;
using System.Text.Json.Serialization;

namespace POT_SEM.Services.Patterns.Strategy
{
    /// <summary>
    /// STRATEGY PATTERN - External API translation implementation
    /// Uses MyMemory free translation API (1000 requests/day)
    /// </summary>
public class ApiTranslationService : ITranslationStrategy
    {
        private readonly HttpClient _httpClient;
        
        public string StrategyName => "External API (MyMemory Translate)";
        
        public ApiTranslationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public async Task<string?> TranslateWordAsync(string word, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return null;
            }
            
            return await TranslateViaMyMemoryAsync(word, sourceLang, targetLang);
        }
        
        public async Task<string?> TranslateSentenceAsync(string sentence, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                return null;
            }
            
            return await TranslateViaMyMemoryAsync(sentence, sourceLang, targetLang);
        }
        
        public async Task<Dictionary<string, string>> TranslateBatchAsync(
            IEnumerable<string> words, 
            string sourceLang, 
            string targetLang)
        {
            var results = new Dictionary<string, string>();
            
            foreach (var word in words)
            {
                var translation = await TranslateWordAsync(word, sourceLang, targetLang);
                if (!string.IsNullOrEmpty(translation))
                {
                    results[word] = translation;
                }
                
                // Small delay to avoid rate limiting
                await Task.Delay(200);
            }
            
            return results;
        }
        
        /// <summary>
        /// Translate using MyMemory API (free, no key required)
        /// </summary>
        private async Task<string?> TranslateViaMyMemoryAsync(string text, string sourceLang, string targetLang)
        {
            try
            {
                var encodedText = HttpUtility.UrlEncode(text);
                var url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={sourceLang}|{targetLang}";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MyMemoryResponse>(json);
                
                return result?.ResponseData?.TranslatedText;
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        // ===== API Response Models =====
        
        private class MyMemoryResponse
        {
            [JsonPropertyName("responseData")]
            public ResponseData? ResponseData { get; set; }
        }
        
        private class ResponseData
        {
            [JsonPropertyName("translatedText")]
            public string? TranslatedText { get; set; }
        }
    }
}