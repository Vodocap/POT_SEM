namespace POT_SEM.Core.Interfaces
{
    public interface IRandomWordService
    {
        /// <summary>
        /// Get random words/topics for a specific language
        /// </summary>
        Task<List<string>> GetRandomWordsAsync(string languageCode, int count);
        
        /// <summary>
        /// Check if service is available
        /// </summary>
        Task<bool> IsAvailableAsync();
        
        string ServiceName { get; }
    }
}