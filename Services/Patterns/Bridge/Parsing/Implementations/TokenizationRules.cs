namespace POT_SEM.Services.Patterns.Bridge.Parsing
{
    /// <summary>
    /// Configuration for tokenization behavior
    /// </summary>
    public class TokenizationRules
    {
        public required string SplitPattern { get; init; }
        public required string PunctuationPattern { get; init; }
        public bool PreserveSpaces { get; init; } = false;
        public Func<char, CharacterClass>? CharacterClassifier { get; init; }
    }
}
