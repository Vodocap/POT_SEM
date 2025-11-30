namespace POT_SEM.Core.Models
{
    /// <summary>
    /// COMPOSITE PATTERN - Root (whole text)
    /// Represents fully processed text ready for display with translations
    /// </summary>
    public class ProcessedText
    {
        public required Text OriginalText { get; init; }
        public required string SourceLanguage { get; init; }
        public required string TargetLanguage { get; init; }
        public required List<ProcessedSentence> Sentences { get; init; }
        public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
        
        // Aggregate properties
        public int TotalWords => Sentences.Sum(s => s.Words.Count);
        public int TotalSentences => Sentences.Count;
        public int UniqueWords => Sentences
            .SelectMany(s => s.Words)
            .Where(w => !w.IsPunctuation)
            .Select(w => w.Normalized)
            .Distinct()
            .Count();
            
        public override string ToString()
        {
            return $"ProcessedText: {TotalSentences} sentences, {TotalWords} words ({SourceLanguage} → {TargetLanguage})";
        }
    }

    /// <summary>
    /// COMPOSITE PATTERN - Branch (sentence containing words)
    /// </summary>
    public class ProcessedSentence
    {
        public required string OriginalText { get; init; }
        public string? Translation { get; set; }
        public required List<ProcessedWord> Words { get; init; }
        public required int Index { get; init; }
        
        public IEnumerable<ProcessedWord> ContentWords => Words.Where(w => !w.IsPunctuation);
        public int WordCount => ContentWords.Count();
        
        public override string ToString()
        {
            return $"Sentence {Index}: {WordCount} words";
        }
    }

    /// <summary>
    /// COMPOSITE PATTERN - Leaf (individual word)
    /// FLYWEIGHT PATTERN - Translation shared across same words
    /// </summary>
    public class ProcessedWord
    {
        // Original data
        public required string Original { get; init; }
        public required string Normalized { get; init; }
        public required int Index { get; init; }
        public bool IsPunctuation { get; init; }
        
        // FLYWEIGHT: Shared translations
        public string? Translation { get; set; }
        public string? Transliteration { get; set; }
        public string? Furigana { get; set; }

        // Flyweight reference to dictionary entry (Wiktionary)
        public POT_SEM.Core.Models.DictionaryEntry? DictionaryEntry { get; set; }

        // Additional metadata for decorators (e.g., hasFurigana)
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        // Metadata
        public int PositionInSentence { get; init; }
        
        public override string ToString()
        {
            return IsPunctuation ? Original : $"{Original} → {Translation ?? "?"}";
        }
    }
}