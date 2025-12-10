using POT_SEM.Core.Models;

namespace POT_SEM.Services.TextProviders.Parsing
{
    /// <summary>
    /// parser for Arabic language
    /// </summary>
    public class ArabicLanguageParser : LanguageParser
    {
        public ArabicLanguageParser(ITextSplitter splitter, IWordTokenizer tokenizer)
            : base(splitter, tokenizer)
        {
        }
        
        protected override List<string> SplitSentences(string text)
        {
            return _splitter.Split(text, @"(?<=[.!?؟])\s+");
        }
        
        protected override List<ProcessedWord> TokenizeWords(string sentence, int index)
        {
            var rules = new TokenizationRules
            {
                SplitPattern = @"(\s+|[،؛,;.!?؟])",
                PunctuationPattern = @"^[،؛,;.!?؟]+$"
            };
            return _tokenizer.Tokenize(sentence, index, rules);
        }
        
        public override string GetLanguageCode() => "ar";
    }
}
