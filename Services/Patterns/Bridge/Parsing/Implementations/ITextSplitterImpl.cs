namespace POT_SEM.Services.Patterns.Bridge.Parsing
{
    /// <summary>
    /// BRIDGE PATTERN - Implementation Interface
    /// Generic text splitting algorithm
    /// </summary>
    public interface ITextSplitterImpl
    {
        List<string> Split(string text, string pattern);
    }
}
