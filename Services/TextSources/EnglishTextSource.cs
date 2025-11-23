using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Text.Json;

namespace POT_SEM.Services.TextSources
{
    public class EnglishTextSource : ILanguageTextSource
    {
        private readonly HttpClient _httpClient;
        private readonly ITopicGenerationStrategy _topicStrategy;
        
        public EnglishTextSource(HttpClient httpClient, ITopicGenerationStrategy topicStrategy)
        {
            _httpClient = httpClient;
            _topicStrategy = topicStrategy;
        }
        
        public string LanguageCode => "en";
        public string LanguageName => "English";
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();
            
            // Get random topics (fetch requested count)
            var topics = await _topicStrategy.GenerateTopicsAsync(
                LanguageCode, 
                criteria.Difficulty, 
                criteria.MaxResults ?? 10); // Use criteria count
            
            Console.WriteLine($"📚 Fetching {topics.Count} English texts for {criteria.Difficulty}");
            
            foreach (var topic in topics)
            {
                try
                {
                    var text = await FetchSingleText(topic, criteria);
                    if (text != null)
                    {
                        texts.Add(text);
                        Console.WriteLine($"   ✅ {topic} ({text.Metadata.EstimatedWordCount} words)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️ Failed: {topic} - {ex.Message}");
                }
            }
            
            Console.WriteLine($"   Total fetched: {texts.Count}/{topics.Count}");
            
            return texts;
        }
        
        private async Task<Text?> FetchSingleText(string topic, TextSearchCriteria criteria)
        {
            // Use Simple Wikipedia for Beginner
            var useSimple = criteria.Difficulty == DifficultyLevel.Beginner;
            var baseUrl = useSimple 
                ? "https://simple.wikipedia.org"
                : "https://en.wikipedia.org";
            
            var url = $"{baseUrl}/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode) return null;
            
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);
            var extract = data.RootElement.GetProperty("extract").GetString() ?? "";
            
            if (string.IsNullOrEmpty(extract)) return null;
            
            // Don't pre-truncate here - let TextProvider handle it
            // Just return the full extract
            
            return new Text
            {
                Title = data.RootElement.GetProperty("title").GetString() ?? topic,
                Content = extract,
                Language = LanguageCode,
                Difficulty = criteria.Difficulty,
                Metadata = new TextMetadata
                {
                    Source = useSimple ? "Simple Wikipedia" : "Wikipedia",
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