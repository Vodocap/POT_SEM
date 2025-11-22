using POT_SEM.Core.Models;

namespace POT_SEM.Core.Interfaces
{
    /// <summary>
    /// STRATEGY PATTERN - Interface pre fetch strategies
    /// </summary>
    public interface ITextFetchStrategy
    {
        string SourceName { get; }
        Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria);
        Task<bool> SupportsTopicAsync(string topic);
    }
}