using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using System.Text.Json;

namespace POT_SEM.Services.TextFetchStrategies
{
    /// <summary>
    /// Fetch strategy pre Project Gutenberg (classic literature)
    /// </summary>
    public class GutenbergStrategy : ITextFetchStrategy
    {
        private readonly HttpClient _httpClient;
        private const string GUTENBERG_API = "https://gutendex.com/books";

        public GutenbergStrategy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string SourceName => "Project Gutenberg";

        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();

            try
            {
                var languageCode = criteria.Language.ToLower();
                var maxResults = criteria.MaxResults ?? 10;

                // Build Gutenberg API URL
                var url = $"{GUTENBERG_API}?languages={languageCode}&page=1&page_size={maxResults}";

                Console.WriteLine($"📚 {SourceName}: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ {SourceName}: API returned {response.StatusCode}");
                    return texts;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(json);

                if (!data.RootElement.TryGetProperty("results", out var results))
                {
                    return texts;
                }

                foreach (var book in results.EnumerateArray())
                {
                    var text = ParseBook(book, criteria);
                    if (text != null)
                    {
                        texts.Add(text);
                    }

                    if (texts.Count >= maxResults)
                    {
                        break;
                    }
                }

                Console.WriteLine($"✅ {SourceName}: Got {texts.Count} books");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {SourceName} error: {ex.Message}");
            }

            return texts;
        }

        private Text? ParseBook(JsonElement book, TextSearchCriteria criteria)
        {
            try
            {
                // Get title
                if (!book.TryGetProperty("title", out var titleElement))
                {
                    return null;
                }

                var title = titleElement.GetString() ?? "Untitled";

                // Get author
                var author = "Unknown Author";
                if (book.TryGetProperty("authors", out var authorsElement))
                {
                    var firstAuthor = authorsElement.EnumerateArray().FirstOrDefault();
                    if (firstAuthor.ValueKind != JsonValueKind.Undefined)
                    {
                        if (firstAuthor.TryGetProperty("name", out var nameElement))
                        {
                            author = nameElement.GetString() ?? "Unknown Author";
                        }
                    }
                }

                // Build content from subjects (Gutenberg API doesn't provide full text summaries)
                var content = BuildContentFromSubjects(book, title);

                // Get book ID for URL
                var bookUrl = "";
                if (book.TryGetProperty("id", out var idElement))
                {
                    var id = idElement.GetInt32();
                    bookUrl = $"https://www.gutenberg.org/ebooks/{id}";
                }

                // Estimate word count (books are typically long)
                var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount < 100)
                {
                    wordCount = 50000; // Typical book length estimate
                }

                return new Text
                {
                    Title = title,
                    Content = content,
                    Language = criteria.Language,
                    Difficulty = criteria.Difficulty, // ✅ Just pass it through
                    Metadata = new TextMetadata
                    {
                        Source = SourceName,
                        Author = author,
                        EstimatedWordCount = wordCount,
                        SourceUrl = bookUrl,
                        Topics = new List<string> { "Classic Literature" }
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to parse book: {ex.Message}");
                return null;
            }
        }

        private string BuildContentFromSubjects(JsonElement book, string title)
        {
            var content = "";

            // Try to get subjects as description
            if (book.TryGetProperty("subjects", out var subjectsElement))
            {
                var subjects = subjectsElement.EnumerateArray()
                    .Select(s => s.GetString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Take(3)
                    .ToList();

                if (subjects.Any())
                {
                    content = $"\"{title}\" is a classic work about: {string.Join(", ", subjects)}. " +
                              $"This literary piece represents significant cultural and historical value.";
                }
            }

            // Fallback
            if (string.IsNullOrEmpty(content))
            {
                content = $"\"{title}\" - A classic literary work from Project Gutenberg's collection of timeless literature.";
            }

            return content;
        }

        public async Task<bool> SupportsTopicAsync(string topic)
        {
            // Gutenberg is a general archive - supports broad topics
            return await Task.FromResult(true);
        }
    }
}