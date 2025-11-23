using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Text.Json;

namespace POT_SEM.Services.TextSources
{
    public class JapaneseTextSource : ILanguageTextSource
    {
        private readonly HttpClient _httpClient;
        private readonly ITopicGenerationStrategy _topicStrategy;
        
        public JapaneseTextSource(HttpClient httpClient, ITopicGenerationStrategy topicStrategy)
        {
            _httpClient = httpClient;
            _topicStrategy = topicStrategy;
        }
        
        public string LanguageCode => "ja";
        public string LanguageName => "Japanese (日本語)";
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();
            
            // Get random topics - use criteria count
            var topics = await _topicStrategy.GenerateTopicsAsync(
                LanguageCode, 
                criteria.Difficulty, 
                criteria.MaxResults ?? 10);
            
            Console.WriteLine($"📚 {topics.Count}個の日本語テキストを取得中 ({criteria.Difficulty})");
            
            foreach (var topic in topics)
            {
                try
                {
                    var text = await FetchSingleText(topic, criteria);
                    if (text != null)
                    {
                        texts.Add(text);
                        Console.WriteLine($"   ✅ {topic} ({text.Metadata.EstimatedWordCount} 文字)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️ エラー: {topic} - {ex.Message}");
                }
            }
            
            Console.WriteLine($"   合計: {texts.Count}/{topics.Count}");
            
            return texts;
        }
        
        private async Task<Text?> FetchSingleText(string topic, TextSearchCriteria criteria)
        {
            var url = $"https://ja.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
            
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);
            var extract = data.RootElement.GetProperty("extract").GetString() ?? "";
            
            if (string.IsNullOrEmpty(extract)) return null;
            
            // Don't pre-truncate - let TextProvider handle it
            // Return full extract
            
            return new Text
            {
                Title = data.RootElement.GetProperty("title").GetString() ?? "タイトルなし",
                Content = extract,
                Language = LanguageCode,
                Difficulty = criteria.Difficulty,
                Metadata = new TextMetadata
                {
                    Source = "Japanese Wikipedia",
                    EstimatedWordCount = extract.Length / 2, // Approximate for Japanese (chars to words)
                    SourceUrl = url
                }
            };
        }
        
        public bool SupportsDifficulty(DifficultyLevel level) => true;
        
        public async Task<List<string>> GetAvailableTopicsAsync()
        {
            return await _topicStrategy.GenerateTopicsAsync(LanguageCode, DifficultyLevel.Intermediate, 10);
        }
    }
}