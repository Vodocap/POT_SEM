using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Dictionary
{
    public class WiktionaryService
    {
        private readonly HttpClient _httpClient;

        public WiktionaryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DictionaryEntry?> LookupAsync(string word, string lang)
        {
            if (string.IsNullOrEmpty(word)) return null;

            var url = $"https://en.wiktionary.org/api/rest_v1/page/definition/{Uri.EscapeDataString(word)}";
            try
            {
                var element = await _httpClient.GetFromJsonAsync<JsonElement?>(url).ConfigureAwait(false);
                if (element == null || element.Value.ValueKind != JsonValueKind.Object) return null;

                var root = element.Value;

                if (!root.TryGetProperty(lang, out var langArray) || langArray.ValueKind != JsonValueKind.Array)
                    return null;

                var meanings = new List<string>();
                string? pos = null;

                foreach (var part in langArray.EnumerateArray())
                {
                    if (part.ValueKind != JsonValueKind.Object) continue;

                    if (part.TryGetProperty("partOfSpeech", out var posEl) && posEl.ValueKind == JsonValueKind.String)
                    {
                        pos ??= posEl.GetString();
                    }

                    if (part.TryGetProperty("definitions", out var defs) && defs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var def in defs.EnumerateArray())
                        {
                            if (def.ValueKind != JsonValueKind.Object) continue;
                            if (def.TryGetProperty("definition", out var defEl) && defEl.ValueKind == JsonValueKind.String)
                            {
                                meanings.Add(defEl.GetString() ?? string.Empty);
                            }
                        }
                    }
                }

                if (!meanings.Any()) return null;

                return new DictionaryEntry
                {
                    Word = word,
                    LanguageCode = lang,
                    PartOfSpeech = pos,
                    Meanings = meanings
                };
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Dictionary<string, DictionaryEntry>> LookupBatchAsync(List<string> words, string lang)
        {
            var results = new Dictionary<string, DictionaryEntry>();
            if (words == null || words.Count == 0) return results;

            // Simple parallelism with degree limit
            var semaphore = new System.Threading.SemaphoreSlim(10);
            var tasks = words.Select(async w =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var entry = await LookupAsync(w, lang).ConfigureAwait(false);
                    if (entry != null)
                    {
                        lock (results)
                        {
                            results[w] = entry;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return results;
        }
    }
}
