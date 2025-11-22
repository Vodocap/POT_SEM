using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Text.Json;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;  

namespace POT_SEM.Services.TextSources
{
    public class SlovakTextSource : ILanguageTextSource
    {
        private readonly HttpClient _httpClient;
        
        public SlovakTextSource(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public string LanguageCode => "sk";
        public string LanguageName => "Slovak (Slovenčina)";
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();
            
            try
            {
                var topics = criteria.Difficulty switch
                {
                    DifficultyLevel.Beginner => new[] { "Slovensko", "Bratislava", "Jedlo" },
                    DifficultyLevel.Intermediate => new[] { "História_Slovenska", "Slovenská_kultúra" },
                    _ => new[] { "Slovenská_literatúra", "Slovenské_dejiny" }
                };
                
                var topic = criteria.Topic ?? topics[Random.Shared.Next(topics.Length)];
                var url = $"https://sk.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonDocument.Parse(json);
                    var extract = data.RootElement.GetProperty("extract").GetString() ?? "";
                    
                    if (!string.IsNullOrEmpty(extract))
                    {
                        if (criteria.Difficulty == DifficultyLevel.Beginner)
                        {
                            extract = string.Join(". ", extract.Split('.').Take(3)) + ".";
                        }
                        
                        texts.Add(new Text
                        {
                            Title = data.RootElement.GetProperty("title").GetString() ?? "Bez názvu",
                            Content = extract,
                            Language = LanguageCode,
                            Difficulty = criteria.Difficulty,
                            Metadata = new TextMetadata
                            {
                                Source = "Slovak Wikipedia",
                                EstimatedWordCount = extract.Split(' ').Length,
                                SourceUrl = url
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            return texts;
        }
        
        public bool SupportsDifficulty(DifficultyLevel level) => true;
        
        public async Task<List<string>> GetAvailableTopicsAsync()
        {
            return new List<string> { "História", "Kultúra", "Veda", "Šport", "Umenie" };
        }
    }
}