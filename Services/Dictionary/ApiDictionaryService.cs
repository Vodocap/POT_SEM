using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Dictionary
{
    /// <summary>
    /// API-based dictionary service using custom AI dictionary API
    /// </summary>
    public class ApiDictionaryService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://wispy-king-8fec.matusmonogram.workers.dev";

        public ApiDictionaryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DictionaryEntry?> LookupAsync(string word, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(word)) return null;

            try
            {
                var requestBody = new
                {
                    word = word,
                    source_lang = sourceLang,
                    target_lang = targetLang
                };

                var response = await _httpClient.PostAsJsonAsync(ApiUrl, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<ApiDictionaryResponse>();
                
                if (result?.Success != true || result.Meanings == null || result.Meanings.Count == 0)
                {
                    return null;
                }

                return new DictionaryEntry
                {
                    Word = result.Word ?? word,
                    LanguageCode = sourceLang,
                    PartOfSpeech = null,
                    Meanings = result.Meanings
                };
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Dictionary<string, DictionaryEntry>> LookupBatchAsync(
            List<string> words, 
            string sourceLang, 
            string targetLang)
        {
            var results = new Dictionary<string, DictionaryEntry>();
            if (words == null || words.Count == 0) return results;

            // Parallel lookups with concurrency limit
            var semaphore = new System.Threading.SemaphoreSlim(10);
            var tasks = words.Select(async word =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var entry = await LookupAsync(word, sourceLang, targetLang);
                    if (entry != null)
                    {
                        lock (results)
                        {
                            results[word] = entry;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);
            return results;
        }

        private class ApiDictionaryResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
            
            [JsonPropertyName("word")]
            public string? Word { get; set; }
            
            [JsonPropertyName("meanings")]
            public List<string>? Meanings { get; set; }
        }
    }
}
