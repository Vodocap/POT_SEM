namespace POT_SEM.Services.Patterns.Flyweight
{
    /// <summary>
    /// FLYWEIGHT PATTERN (Gang of Four) - Intrinsic State
    /// Immutable shared object representing a word across all contexts
    /// Contains only intrinsic (shared) state - the word itself and its translations
    /// Extrinsic state (position, sentence index) is passed as parameters
    /// </summary>
    public class WordFlyweight
    {
        // Intrinsic state (shared across all uses of this word)
        public string Text { get; }
        public string Normalized { get; }
        public string SourceLanguage { get; }
        public string TargetLanguage { get; }
        
        // Cached translations (intrinsic - same for all occurrences of this word)
        public string? Translation { get; internal set; }
        public string? Transliteration { get; internal set; }
        public string? Furigana { get; internal set; }
        public POT_SEM.Core.Models.DictionaryEntry? DictionaryEntry { get; internal set; }
        
        // Metadata about the flyweight itself
        public int SharedUseCount { get; internal set; }
        public DateTime FirstUsed { get; }
        public DateTime LastAccessed { get; internal set; }
        
        internal WordFlyweight(string text, string normalized, string sourceLang, string targetLang)
        {
            Text = text;
            Normalized = normalized;
            SourceLanguage = sourceLang;
            TargetLanguage = targetLang;
            SharedUseCount = 0;
            FirstUsed = DateTime.UtcNow;
            LastAccessed = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Display word with extrinsic state (position, context)
        /// This is the Gang of Four approach: intrinsic state stored in flyweight,
        /// extrinsic state passed as parameters
        /// </summary>
        public string Display(int position, bool isPunctuation)
        {
            LastAccessed = DateTime.UtcNow;
            SharedUseCount++;
            
            if (isPunctuation)
                return Text;
            
            return Translation != null 
                ? $"{Text} → {Translation}" 
                : Text;
        }
        
        public override string ToString()
        {
            return $"Flyweight[{Normalized}] shared {SharedUseCount} times";
        }
    }
}
