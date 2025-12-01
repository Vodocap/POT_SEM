using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.Strategy.Topic
{
    /// <summary>
    /// ⚡ INSTANT topic generation - no API calls!
    /// </summary>
    public class StaticTopicStrategy : ITopicGenerationStrategy
    {
        private static readonly Dictionary<string, List<string>> TopicsByLanguage = new()
        {
            ["en"] = new() 
            { 
                "Technology", "Science", "History", "Geography", "Biology", 
                "Physics", "Mathematics", "Literature", "Art", "Music", 
                "Sports", "Politics", "Economy", "Culture", "Environment",
                "Medicine", "Psychology", "Philosophy", "Astronomy", "Chemistry"
            },
            ["sk"] = new() 
            { 
                "Technológia", "Veda", "História", "Geografia", "Biológia", 
                "Fyzika", "Matematika", "Literatúra", "Umenie", "Hudba", 
                "Šport", "Politika", "Ekonomika", "Kultúra", "Životné prostredie",
                "Medicína", "Psychológia", "Filozofia", "Astronómia", "Chémia"
            },
            ["ar"] = new() 
            { 
                "التكنولوجيا", "العلوم", "التاريخ", "الجغرافيا", "علم الأحياء", 
                "الفيزياء", "الرياضيات", "الأدب", "الفن", "الموسيقى", 
                "الرياضة", "السياسة", "الاقتصاد", "الثقافة", "البيئة",
                "الطب", "علم النفس", "الفلسفة", "علم الفلك", "الكيمياء"
            },
            ["ja"] = new() 
            { 
                "技術", "科学", "歴史", "地理", "生物学", 
                "物理学", "数学", "文学", "芸術", "音楽", 
                "スポーツ", "政治", "経済", "文化", "環境",
                "医学", "心理学", "哲学", "天文学", "化学"
            }
        };
        
        public string StrategyName => "Static Topics (Instant)";
        
        public Task<List<string>> GenerateTopicsAsync(
            string languageCode, 
            DifficultyLevel difficulty, 
            int count)
        {
            if (!TopicsByLanguage.TryGetValue(languageCode.ToLower(), out var topics))
            {
                topics = TopicsByLanguage["en"]; // Fallback to English
            }
            
            // Shuffle and take requested count
            var shuffled = topics
                .OrderBy(_ => Random.Shared.Next())
                .Take(count)
                .ToList();
            
            return Task.FromResult(shuffled);
        }
    }
}