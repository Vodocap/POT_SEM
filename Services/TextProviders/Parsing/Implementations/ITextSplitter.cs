namespace POT_SEM.Services.TextProviders.Parsing
{
    /// <summary>
    /// BRIDGE PATTERN - Implementation Interface
    /// Generic text splitting algorithm
    /// </summary>
    public interface ITextSplitter
    {
        List<string> Split(string text, string pattern);
    }
}
