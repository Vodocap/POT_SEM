using System.Text.RegularExpressions;

namespace POT_SEM.Services.TextProviders.Parsing
{
    /// <summary>
    /// BRIDGE PATTERN - Concrete Implementation
    /// Regex-based text splitting (works for most languages)
    /// </summary>
    public class RegexTextSplitter : ITextSplitter
    {
        public List<string> Split(string text, string pattern)
        {
            return Regex.Split(text, pattern)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
    }
}
