using POT_SEM.Core.Services;
using System.Text.Json;

namespace POT_SEM.Services.RandomWordServices
{
    /// <summary>
    /// Uses Wikipedia Random Page API to get random topics
    /// Works for ALL languages, completely free, no limits!
    /// </summary>
    public class WikipediaRandomWordService : IRandomWordService
    {
        private readonly HttpClient _httpClient;
        
        private static readonly Dictionary<string, string> WikipediaBaseUrls = new()
        {
            ["en"] = "https://en.wikipedia.org",
            ["sk"] = "https://sk.wikipedia.org",
            ["ar"] = "https://ar.wikipedia.org",
            ["ru"] = "https://ru.wikipedia.org",
            ["ja"] = "https://ja.wikipedia.org",
            ["de"] = "https://de.wikipedia.org",
            ["fr"] = "https://fr.wikipedia.org",
            ["es"] = "https://es.wikipedia.org",
            ["it"] = "https://it.wikipedia.org",
            ["pl"] = "https://pl.wikipedia.org",
            ["cs"] = "https://cs.wikipedia.org",
            ["hu"] = "https://hu.wikipedia.org",
            ["uk"] = "https://uk.wikipedia.org"
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
                Console.WriteLine($"⚠️ {ServiceName}: Language '{languageCode}' not supported, using English");
                baseUrl = WikipediaBaseUrls["en"];
            }
            
            Console.WriteLine($"🎲 {ServiceName}: Fetching {count} random topics for {languageCode}...");
            
            // Wikipedia random API endpoint
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
                            // Clean up title (remove parentheses, disambiguation)
                            var cleanTitle = CleanTitle(title);
                            
                            if (!string.IsNullOrEmpty(cleanTitle) && !words.Contains(cleanTitle))
                            {
                                words.Add(cleanTitle);
                                Console.WriteLine($"   ✅ [{i + 1}/{count}] {cleanTitle}");
                            }
                            else
                            {
                                i--; // Try again if duplicate or empty
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠️ HTTP {response.StatusCode}");
                        i--; // Retry
                    }
                    
                    // Small delay to avoid rate limiting
                    if (i < count - 1)
                    {
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error: {ex.Message}");
                    i--; // Retry
                }
            }
            
            Console.WriteLine($"✅ Generated {words.Count} random topics");
            
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
            
            // Remove Wikipedia meta pages
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