using POT_SEM.Core.Models;

namespace POT_SEM.Services.TextProviders.Parsing
{
    public class JapaneseLanguageParser : LanguageParser
    {
        public JapaneseLanguageParser(ITextSplitter splitter, IWordTokenizerImpl tokenizer)
            : base(splitter, tokenizer)
        {
        }
        
        protected override List<string> SplitSentences(string text)
        {
            return _splitter.Split(text, @"(?<=[。！？])");
        }
        
        protected override List<ProcessedWord> TokenizeWords(string sentence, int index)
        {
            var rules = new TokenizationRules
            {
                SplitPattern = "", // Not used for script-based
                PunctuationPattern = "", // Not used for script-based
                CharacterClassifier = ClassifyJapaneseCharacter
            };
            return _tokenizer.Tokenize(sentence, index, rules);
        }
        
        private CharacterClass ClassifyJapaneseCharacter(char ch)
        {
            if (char.IsPunctuation(ch) || "、。！？".Contains(ch))
                return CharacterClass.Punctuation;
            
            if (ch >= '\u3040' && ch <= '\u309F') // Hiragana
                return CharacterClass.Syllabic;
            
            if (ch >= '\u30A0' && ch <= '\u30FF') // Katakana
                return CharacterClass.Syllabic;
            
            if (ch >= '\u4E00' && ch <= '\u9FFF') // Kanji
                return CharacterClass.Ideographic;
            
            return CharacterClass.Other;
        }
        
        public override string GetLanguageCode() => "ja";
    }
}
