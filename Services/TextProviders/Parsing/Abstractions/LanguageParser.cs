using POT_SEM.Core.Models;

namespace POT_SEM.Services.TextProviders.Parsing
{
    /// <summary>
    /// Language-specific parser that uses generic implementations
    /// </summary>
    public abstract class LanguageParser
    {
        protected readonly ITextSplitter _splitter;
        protected readonly IWordTokenizerImpl _tokenizer;
        
        protected LanguageParser(ITextSplitter splitter, IWordTokenizerImpl tokenizer)
        {
            _splitter = splitter;
            _tokenizer = tokenizer;
        }
        
        public ProcessedText ParseText(Text originalText, string targetLang)
        {
            var sentences = new List<ProcessedSentence>();
            var sentenceTexts = SplitSentences(originalText.Content);
            
            for (int i = 0; i < sentenceTexts.Count; i++)
            {
                var words = TokenizeWords(sentenceTexts[i], i);
                sentences.Add(new ProcessedSentence
                {
                    OriginalText = sentenceTexts[i],
                    Words = words,
                    Index = i
                });
            }
            
            return new ProcessedText
            {
                OriginalText = originalText,
                SourceLanguage = GetLanguageCode(),
                TargetLanguage = targetLang,
                Sentences = sentences
            };
        }
        
        protected abstract List<string> SplitSentences(string text);
        protected abstract List<ProcessedWord> TokenizeWords(string sentence, int index);
        public abstract string GetLanguageCode();
    }
}
