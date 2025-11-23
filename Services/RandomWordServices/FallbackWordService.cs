using POT_SEM.Core.Services;

namespace POT_SEM.Services.RandomWordServices
{
    /// <summary>
    /// Fallback service when APIs are unavailable
    /// Minimal word pool for basic functionality
    /// </summary>
    public class FallbackWordService : IRandomWordService
    {
        private static readonly Dictionary<string, List<string>> MinimalDictionary = new()
        {
            ["en"] = new() 
            { 
                "Cat", "Dog", "Water", "Sun", "Tree", "Book", "House", "Music", 
                "Science", "Technology", "Art", "History", "Nature", "Culture",
                "Philosophy", "Mathematics", "Physics", "Literature", "Economy", "Psychology"
            },
            ["sk"] = new() 
            { 
                "Mačka", "Pes", "Voda", "Slnko", "Strom", "Kniha", "Dom", "Hudba",
                "Veda", "Technológia", "Umenie", "História", "Príroda", "Kultúra",
                "Filozofia", "Matematika", "Fyzika", "Literatúra", "Ekonómia", "Psychológia"
            },
            ["ar"] = new() 
            { 
                "قطة", "كلب", "ماء", "شمس", "شجرة", "كتاب", "بيت", "موسيقى",
                "علم", "تكنولوجيا", "فن", "تاريخ", "طبيعة", "ثقافة",
                "فلسفة", "رياضيات", "فيزياء", "أدب", "اقتصاد", "علم_النفس"
            },
            ["ja"] = new() 
            { 
                "猫", "犬", "水", "太陽", "木", "本", "家", "音楽",
                "科学", "技術", "芸術", "歴史", "自然", "文化",
                "哲学", "数学", "物理学", "文学", "経済", "心理学"
            }
        };
        
        public string ServiceName => "Fallback Word Pool";
        
        public Task<List<string>> GetRandomWordsAsync(string languageCode, int count)
        {
            Console.WriteLine($"⚠️ {ServiceName}: Using minimal word pool");
            
            if (!MinimalDictionary.TryGetValue(languageCode.ToLower(), out var words))
            {
                words = MinimalDictionary["en"];
            }
            
            var randomWords = words
                .OrderBy(_ => Random.Shared.Next())
                .Take(count)
                .ToList();
            
            Console.WriteLine($"   ✅ Selected: {string.Join(", ", randomWords)}");
            
            return Task.FromResult(randomWords);
        }
        
        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(true); // Always available
        }
    }
}