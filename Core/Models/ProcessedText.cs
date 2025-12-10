namespace POT_SEM.Core.Models
{
    /// <summary>
    /// Fully processed text ready for display with translations.
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
    /// Sentence containing translated words.
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
    /// Individual word with translation data.
    /// </summary>
    public class ProcessedWord
    {
        // Context-specific data (varies per occurrence)
        public required int Index { get; init; }
        public bool IsPunctuation { get; init; }
        public int PositionInSentence { get; init; }
        
        // Shared word data (same across all occurrences)
        public required string Original { get; init; }
        public required string Normalized { get; init; }
        
        // Translation data (populated by flyweight factory)
        public string? Translation { get; set; }
        public string? Transliteration { get; set; }
        public string? Furigana { get; set; }
        public POT_SEM.Core.Models.DictionaryEntry? DictionaryEntry { get; set; }

        // Additional metadata for decorators (extrinsic)
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        // Internal reference to flyweight key (for factory tracking)
        internal string? FlyweightKey { get; set; }
        
        public override string ToString()
        {
            return IsPunctuation ? Original : $"{Original} → {Translation ?? "?"}";
        }
    }
}