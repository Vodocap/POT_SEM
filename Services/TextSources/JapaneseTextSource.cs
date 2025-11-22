using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Text.Json;

namespace POT_SEM.Services.TextSources
{
    public class JapaneseTextSource : ILanguageTextSource
    {
        private readonly HttpClient _httpClient;
        
        public JapaneseTextSource(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public string LanguageCode => "ja";
        public string LanguageName => "Japanese (日本語)";
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();
            
            try
            {
                var topics = criteria.Difficulty switch
                {
                    DifficultyLevel.Beginner => new[] { "日本", "食べ物", "家族" },
                    DifficultyLevel.Intermediate => new[] { "日本の歴史", "日本文化" },
                    _ => new[] { "日本文学", "哲学" }
                };
                
                var topic = criteria.Topic ?? topics[Random.Shared.Next(topics.Length)];
                var url = $"https://ja.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
                
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
                            extract = string.Join("。", extract.Split('。').Take(2)) + "。";
                        }
                        
                        texts.Add(new Text
                        {
                            Title = data.RootElement.GetProperty("title").GetString() ?? "タイトルなし",
                            Content = extract,
                            Language = LanguageCode,
                            Difficulty = criteria.Difficulty,
                            Metadata = new TextMetadata
                            {
                                Source = "Japanese Wikipedia",
                                EstimatedWordCount = extract.Length / 2, // Approximate for Japanese
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
            return new List<string> { "歴史", "文化", "科学", "スポーツ", "芸術" };
        }
    }
}