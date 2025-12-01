using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.Bridge.Parsing
{
    /// <summary>
    /// BRIDGE PATTERN - Concrete Implementation
    /// Script-based tokenization (for Japanese, Chinese - no spaces)
    /// </summary>
    public class ScriptBasedTokenizer : IWordTokenizerImpl
    {
        public List<ProcessedWord> Tokenize(string sentence, int sentenceIndex, TokenizationRules rules)
        {
            if (rules.CharacterClassifier == null)
            {
                throw new InvalidOperationException("ScriptBasedTokenizer requires CharacterClassifier");
            }
            
            var words = new List<ProcessedWord>();
            int position = 0;
            
            var currentWord = "";
            var currentClass = rules.CharacterClassifier(sentence.FirstOrDefault());
            
            foreach (var ch in sentence)
            {
                var charClass = rules.CharacterClassifier(ch);
                
                // Same class: continue word
                if (charClass == currentClass && charClass != CharacterClass.Punctuation)
                {
                    currentWord += ch;
                }
                // Different class or punctuation: end word
                else
                {
                    if (!string.IsNullOrEmpty(currentWord))
                    {
                        words.Add(new ProcessedWord
                        {
                            Original = currentWord,
                            Normalized = currentWord,
                            Index = words.Count,
                            PositionInSentence = position++,
                            IsPunctuation = currentClass == CharacterClass.Punctuation
                        });
                    }
                    
                    currentWord = ch.ToString();
                    currentClass = charClass;
                }
            }
            
            // Add last word
            if (!string.IsNullOrEmpty(currentWord))
            {
                words.Add(new ProcessedWord
                {
                    Original = currentWord,
                    Normalized = currentWord,
                    Index = words.Count,
                    PositionInSentence = position,
                    IsPunctuation = currentClass == CharacterClass.Punctuation
                });
            }
            
            return words;
        }
    }
}
