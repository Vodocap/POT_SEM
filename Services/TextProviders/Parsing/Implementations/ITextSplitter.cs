namespace POT_SEM.Services.TextProviders.Parsing
{
    /// <summary>
    /// Implementation Interface
    /// Generic text splitting algorithm
    /// </summary>
    public interface ITextSplitter
    {
        List<string> Split(string text, string pattern);
    }
}
