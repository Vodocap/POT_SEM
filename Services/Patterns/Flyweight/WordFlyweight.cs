namespace POT_SEM.Services.Patterns.Flyweight
{
    /// <summary>
    /// FLYWEIGHT PATTERN - Intrinsic State
    /// Immutable shared object representing a word and its translation
    /// Contains ONLY intrinsic state: word + translation (shared across all contexts)
    /// </summary>
    public class WordFlyweight
    {
        public string Text { get; }
        public string Normalized { get; }
        public string SourceLanguage { get; }
        public string TargetLanguage { get; }
        public string? Translation { get; internal set; }
        public DateTime LastAccessed { get; internal set; }
        
        internal WordFlyweight(string text, string normalized, string sourceLang, string targetLang)
        {
            Text = text;
            Normalized = normalized;
            SourceLanguage = sourceLang;
            TargetLanguage = targetLang;
            LastAccessed = DateTime.UtcNow;
        }
    }
}
