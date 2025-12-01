using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.Bridge.Parsing;

namespace POT_SEM.Services.Patterns.Composite
{
    /// <summary>
    /// Builds COMPOSITE PATTERN structure using BRIDGE PATTERN for language parsing
    /// Creates tree: ProcessedText → ProcessedSentence → ProcessedWord
    /// Bridge separates language abstractions from parsing implementations
    /// </summary>
    public class TextParser
    {
        private readonly LanguageParser _languageParser;
        
        public TextParser(LanguageParser languageParser)
        {
            _languageParser = languageParser;
        }
        
        /// <summary>
        /// Parse text into composite structure
        /// </summary>
        public ProcessedText ParseText(Text originalText, string sourceLang, string targetLang)
        {
            return _languageParser.ParseText(originalText, targetLang);
        }
        
        /// <summary>
        /// Extract unique words from processed text (for translation)
        /// </summary>
        public List<string> ExtractUniqueWords(ProcessedText processedText)
        {
            return processedText.Sentences
                .SelectMany(s => s.Words)
                .Where(w => !w.IsPunctuation)
                .Select(w => w.Normalized)
                .Distinct()
                .ToList();
        }
    }
    
    /// <summary>
    /// FACTORY METHOD - Creates parser with appropriate Bridge configuration
    /// </summary>
    public class TextParserFactory
    {
        public static TextParser CreateParser(string languageCode)
        {
            var languageParser = LanguageParserFactory.CreateParser(languageCode);
            return new TextParser(languageParser);
        }
    }
}
