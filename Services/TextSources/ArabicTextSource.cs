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
        private readonly Dictionary<DifficultyLevel, Func<TextSearchCriteria, Task<List<Text>>>> _strategies;
        
        public ArabicTextSource(HttpClient httpClient)
        {
            _httpClient = httpClient;
            
            _strategies = new()
            {
                [DifficultyLevel.Beginner] = FetchBeginnerTexts,
                [DifficultyLevel.Intermediate] = FetchIntermediateTexts,
                [DifficultyLevel.Advanced] = FetchAdvancedTexts
            };
        }
        
        public string LanguageCode => "ar";
        public string LanguageName => "Arabic (العربية)";
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            if (_strategies.TryGetValue(criteria.Difficulty, out var strategy))
            {
                Console.WriteLine($"📚 {LanguageName}: Fetching {criteria.Difficulty} texts");
                return await strategy(criteria);
            }
            
            return new List<Text>();
        }
        
        public bool SupportsDifficulty(DifficultyLevel level) => _strategies.ContainsKey(level);
        
        public async Task<List<string>> GetAvailableTopicsAsync()
        {
            return new List<string> { "الثقافة", "التاريخ", "العلوم", "الأدب", "التعليم" };
        }
        
        private async Task<List<Text>> FetchBeginnerTexts(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();
            
            try
            {
                var topics = new[] { "الأطفال", "الأسرة", "الطعام" };
                var topic = criteria.Topic ?? topics[Random.Shared.Next(topics.Length)];
                
                var url = $"https://ar.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonDocument.Parse(json);
                    var extract = data.RootElement.GetProperty("extract").GetString() ?? "";
                    
                    if (!string.IsNullOrEmpty(extract))
                    {
                        var simplified = string.Join(". ", extract.Split('.').Take(3)) + ".";
                        
                        texts.Add(new Text
                        {
                            Title = data.RootElement.GetProperty("title").GetString() ?? "بدون عنوان",
                            Content = simplified,
                            Language = LanguageCode,
                            Difficulty = criteria.Difficulty,
                            Metadata = new TextMetadata
                            {
                                Source = "Arabic Wikipedia (Beginner)",
                                EstimatedWordCount = simplified.Split(' ').Length,
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
        
        private async Task<List<Text>> FetchIntermediateTexts(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();
            
            try
            {
                var topic = criteria.Topic ?? "التكنولوجيا";
                var url = $"https://ar.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(topic)}";
                
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
                            Title = data.RootElement.GetProperty("title").GetString() ?? "بدون عنوان",
                            Content = extract,
                            Language = LanguageCode,
                            Difficulty = criteria.Difficulty,
                            Metadata = new TextMetadata
                            {
                                Source = "Arabic Wikipedia",
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
        
        private async Task<List<Text>> FetchAdvancedTexts(TextSearchCriteria criteria)
        {
            return await FetchIntermediateTexts(criteria);
        }
    }
}