using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Text.Json;

namespace POT_SEM.Services.TextFetchStrategies
{
    /// <summary>
    /// Fetch strategy pre Wikipedia (akýkoľvek jazyk)
    /// </summary>
    public class WikipediaStrategy : ITextFetchStrategy
    {
        private readonly HttpClient _httpClient;
        private readonly string _languageCode;

        public WikipediaStrategy(HttpClient httpClient, string languageCode = "en")
        {
            _httpClient = httpClient;
            _languageCode = languageCode.ToLower();
        }

        public string SourceName => $"Wikipedia ({_languageCode.ToUpper()})";

        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();

            try
            {
                var topic = criteria.Topic ?? "Random";
                var url = $"https://{_languageCode}.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return texts;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(json);

                var title = data.RootElement.GetProperty("title").GetString() ?? "Untitled";
                var extract = data.RootElement.GetProperty("extract").GetString() ?? "";

                if (string.IsNullOrEmpty(extract))
                {
                    return texts;
                }

                var wordCount = extract.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                texts.Add(new Text
                {
                    Title = title,
                    Content = extract,
                    Language = criteria.Language,
                    Difficulty = criteria.Difficulty,
                    Metadata = new TextMetadata
                    {
                        Source = SourceName,
                        EstimatedWordCount = wordCount,
                        SourceUrl = $"https://{_languageCode}.wikipedia.org/wiki/{Uri.EscapeDataString(topic)}"
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {SourceName} error: {ex.Message}");
            }

            return texts;
        }

        public async Task<bool> SupportsTopicAsync(string topic)
        {
            try
            {
                var url = $"https://{_languageCode}.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}