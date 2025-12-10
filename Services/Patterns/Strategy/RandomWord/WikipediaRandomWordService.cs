using System.Text.Json;

namespace POT_SEM.Services.Patterns.Strategy.RandomWord
{
    /// <summary>
    /// Uses Wikipedia Random Page API to get random topics
    /// </summary>
    public class WikipediaRandomWordService
    {
        private readonly HttpClient _httpClient;
        
        private static readonly Dictionary<string, string> WikipediaBaseUrls = new()
        {
            ["en"] = "https://en.wikipedia.org",
            ["sk"] = "https://sk.wikipedia.org",
            ["ar"] = "https://ar.wikipedia.org",
            ["ja"] = "https://ja.wikipedia.org"
        };
        
        public WikipediaRandomWordService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public string ServiceName => "Wikipedia Random API";
        
        public async Task<List<string>> GetRandomWordsAsync(string languageCode, int count)
        {
            var words = new List<string>();
            
            if (!WikipediaBaseUrls.TryGetValue(languageCode.ToLower(), out var baseUrl))
            {
                baseUrl = WikipediaBaseUrls["en"];
            }
            
            var randomUrl = $"{baseUrl}/api/rest_v1/page/random/summary";
            
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(randomUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonDocument.Parse(json);
                        
                        var title = data.RootElement.GetProperty("title").GetString();
                        
                        if (!string.IsNullOrEmpty(title))
                        {
                            var cleanTitle = CleanTitle(title);
                            
                            if (!string.IsNullOrEmpty(cleanTitle) && !words.Contains(cleanTitle))
                            {
                                words.Add(cleanTitle);
                            }
                            else
                            {
                                i--; // Try again if duplicate or empty
                            }
                        }
                    }
                    else
                    {
                        i--; // Retry
                    }
                    
                    if (i < count - 1)
                    {
                        await Task.Delay(50);
                    }
                }
                catch
                {
                    i--; 
                }
            }
            
            return words;
        }
        
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    "https://en.wikipedia.org/api/rest_v1/page/random/summary",
                    HttpCompletionOption.ResponseHeadersRead);
                
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        private string CleanTitle(string title)
        {
            // Remove disambiguation and other metadata
            var cleanTitle = title.Split('(')[0].Trim();
            
            // Remove "List of" articles
            if (cleanTitle.StartsWith("List of ", StringComparison.OrdinalIgnoreCase) ||
                cleanTitle.StartsWith("Zoznam ", StringComparison.OrdinalIgnoreCase) ||
                cleanTitle.StartsWith("قائمة ", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
            
            if (cleanTitle.Contains("Wikipedia:") || 
                cleanTitle.Contains("Category:") ||
                cleanTitle.Contains("Portal:") ||
                cleanTitle.Contains("Template:"))
            {
                return string.Empty;
            }
            
            return cleanTitle;
        }
    }
}