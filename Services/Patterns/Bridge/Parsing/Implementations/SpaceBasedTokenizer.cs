using POT_SEM.Core.Models;
using System.Text.RegularExpressions;

namespace POT_SEM.Services.Patterns.Bridge.Parsing
{
    /// <summary>
    /// BRIDGE PATTERN - Concrete Implementation
    /// Space-based word tokenization (for Latin, Arabic, etc.)
    /// </summary>
    public class SpaceBasedTokenizer : IWordTokenizerImpl
    {
        public List<ProcessedWord> Tokenize(string sentence, int sentenceIndex, TokenizationRules rules)
        {
            var words = new List<ProcessedWord>();
            var tokens = Regex.Split(sentence, rules.SplitPattern)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            
            int position = 0;
            
            foreach (var token in tokens)
            {
                var isPunctuation = Regex.IsMatch(token, rules.PunctuationPattern);
                words.Add(new ProcessedWord
                {
                    Original = token,
                    Normalized = NormalizeToken(token, isPunctuation),
                    Index = words.Count,
                    PositionInSentence = position++,
                    IsPunctuation = isPunctuation
                });
            }
            
            return words;
        }
        
        private string NormalizeToken(string token, bool isPunctuation)
        {
            return isPunctuation ? token : token.ToLower().Trim();
        }
    }
}
