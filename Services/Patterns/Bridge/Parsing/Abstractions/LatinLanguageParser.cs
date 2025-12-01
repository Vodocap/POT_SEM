using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.Bridge.Parsing
{
    /// <summary>
    /// BRIDGE PATTERN - Refined Abstraction for Latin-based languages
    /// </summary>
    public class LatinLanguageParser : LanguageParser
    {
        public LatinLanguageParser(ITextSplitterImpl splitter, IWordTokenizerImpl tokenizer)
            : base(splitter, tokenizer)
        {
        }
        
        protected override List<string> SplitSentences(string text)
        {
            return _splitter.Split(text, @"(?<=[.!?])\s+");
        }
        
        protected override List<ProcessedWord> TokenizeWords(string sentence, int index)
        {
            var rules = new TokenizationRules
            {
                SplitPattern = @"(\s+|[,;:.!?])",
                PunctuationPattern = @"^[,;:.!?]+$"
            };
            return _tokenizer.Tokenize(sentence, index, rules);
        }
        
        public override string GetLanguageCode() => "latin";
    }
}
