namespace POT_SEM.Core.Models
{
    public class Text
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Language { get; set; } = "";
        public DifficultyLevel Difficulty { get; set; }
        public TextMetadata Metadata { get; set; } = new();
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced
    }
    
    public class TextMetadata
    {
        public string? Author { get; set; }
        public string? Source { get; set; }
        public int EstimatedWordCount { get; set; }
        public int EstimatedReadingTimeMinutes { get; set; }
        public List<string> Topics { get; set; } = new();
        public string? SourceUrl { get; set; }
    }
    
    public class TextSearchCriteria
    {
        public DifficultyLevel Difficulty { get; set; }
        public string Language { get; set; } = "";
        public string? Topic { get; set; }
        public int MaxWordCount { get; set; } = int.MaxValue;
        public int MinWordCount { get; set; } = 0;
        public int? MaxResults { get; set; }
    }
}