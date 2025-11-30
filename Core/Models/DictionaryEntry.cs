using System.Collections.Generic;

namespace POT_SEM.Core.Models
{
    // FLYWEIGHT - immutable, shared dictionary entry
    public class DictionaryEntry
    {
        public string Word { get; init; } = string.Empty;
        public string LanguageCode { get; init; } = string.Empty;
        public string? PartOfSpeech { get; init; }
        public List<string> Meanings { get; init; } = new();
        public List<string>? Examples { get; init; }
    }
}
