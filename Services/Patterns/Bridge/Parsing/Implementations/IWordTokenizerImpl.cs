using POT_SEM.Core.Models;

namespace POT_SEM.Services.Patterns.Bridge.Parsing
{
    /// <summary>
    /// BRIDGE PATTERN - Implementation Interface
    /// Generic word tokenization algorithm
    /// </summary>
    public interface IWordTokenizerImpl
    {
        List<ProcessedWord> Tokenize(string sentence, int sentenceIndex, TokenizationRules rules);
    }
}
