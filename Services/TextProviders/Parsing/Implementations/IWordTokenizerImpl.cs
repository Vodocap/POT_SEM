using POT_SEM.Core.Models;

namespace POT_SEM.Services.TextProviders.Parsing
{
    /// <summary>
    /// Implementation Interface
    /// Generic word tokenization algorithm
    /// </summary>
    public interface IWordTokenizer
    {
        List<ProcessedWord> Tokenize(string sentence, int sentenceIndex, TokenizationRules rules);
    }
}
