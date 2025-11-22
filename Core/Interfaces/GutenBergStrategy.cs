using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.FetchStrategies
{
    public class GutenbergStrategy : ITextFetchStrategy
    {
        private readonly HttpClient _httpClient;
        
        public GutenbergStrategy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public string SourceName => "Project Gutenberg";
        
        public async Task<List<Text>> FetchTextsAsync(TextSearchCriteria criteria)
        {
            var texts = new List<Text>();
            
            // Simplified - real implementation would fetch from Gutenberg API
            texts.Add(new Text
            {
                Title = "Sample Classic Literature",
                Content = "This is a sample text from classic literature. " +
                         "In real implementation, this would fetch from Project Gutenberg. " +
                         "Advanced texts include complex vocabulary and longer sentences.",
                Language = criteria.Language,
                Difficulty = criteria.Difficulty,
                Metadata = new TextMetadata
                {
                    Source = SourceName,
                    Author = "Sample Author",
                    EstimatedWordCount = 500,
                    SourceUrl = "https://www.gutenberg.org"
                }
            });
            
            return texts;
        }
        
        public async Task<bool> SupportsTopicAsync(string topic)
        {
            return true;
        }
    }
}