using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Text.Json;

namespace POT_SEM.Services.TextSources
{
    /// <summary>
    /// BRIDGE IMPLEMENTATION - Arabic texts (العربية)
    /// </summary>
    public class ArabicTextSource : ILanguageTextSource
    {
        private readonly HttpClient _httpClient;
        private readonly ITopicGenerationStrategy _topicStrategy;
        
        public ArabicTextSource(HttpClient httpClient, ITopicGenerationStrategy topicStrategy)
        {
            _httpClient = httpClient;
            _topicStrategy = topicStrategy;
        }
        
        public string LanguageCode => "ar";
        public string LanguageName => "Arabic (العربية)";
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();
            
            // Get random topics - use criteria count
            var topics = await _topicStrategy.GenerateTopicsAsync(
                LanguageCode, 
                criteria.Difficulty, 
                criteria.MaxResults ?? 10);
            
            Console.WriteLine($"📚 جلب {topics.Count} نصوص عربية لـ {criteria.Difficulty}");
            
            foreach (var topic in topics)
            {
                try
                {
                    var text = await FetchSingleText(topic, criteria);
                    if (text != null)
                    {
                        texts.Add(text);
                        Console.WriteLine($"   ✅ {topic} ({text.Metadata.EstimatedWordCount} كلمات)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️ فشل: {topic} - {ex.Message}");
                }
            }
            
            Console.WriteLine($"   المجموع: {texts.Count}/{topics.Count}");
            
            return texts;
        }
        
        private async Task<Text?> FetchSingleText(string topic, TextSearchCriteria criteria)
        {
            var url = $"https://ar.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
            
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
                Title = data.RootElement.GetProperty("title").GetString() ?? "بدون عنوان",
                Content = extract,
                Language = LanguageCode,
                Difficulty = criteria.Difficulty,
                Metadata = new TextMetadata
                {
                    Source = "Arabic Wikipedia",
                    EstimatedWordCount = extract.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
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