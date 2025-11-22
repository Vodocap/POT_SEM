using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Text.Json;

namespace POT_SEM.Services.FetchStrategies
{
    public class WikipediaStrategy : ITextFetchStrategy
    {
        private readonly HttpClient _httpClient;
        
        public WikipediaStrategy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public string SourceName => "Wikipedia";
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();
            
            try
            {
                var topics = new[] { "Technology", "Science", "History", "Culture" };
                var topic = criteria.Topic ?? topics[Random.Shared.Next(topics.Length)];
                
                var url = $"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonDocument.Parse(json);
                    var extract = data.RootElement.GetProperty("extract").GetString() ?? "";
                    
                    if (!string.IsNullOrEmpty(extract))
                    {
                        texts.Add(new Text
                        {
                            Title = data.RootElement.GetProperty("title").GetString() ?? "Untitled",
                            Content = extract,
                            Language = criteria.Language,
                            Difficulty = criteria.Difficulty,
                            Metadata = new TextMetadata
                            {
                                Source = SourceName,
                                EstimatedWordCount = extract.Split(' ').Length,
                                SourceUrl = url
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {SourceName}: {ex.Message}");
            }
            
            return texts;
        }
        
        public async Task<bool> SupportsTopicAsync(string topic)
        {
            try
            {
                var url = $"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
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