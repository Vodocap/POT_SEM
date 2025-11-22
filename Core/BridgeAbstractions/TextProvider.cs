using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;

namespace POT_SEM.Core.BridgeAbstractions
{
    /// <summary>
    /// BRIDGE ABSTRACTION
    /// Abstraktná základná trieda pre text providers podľa obtiažnosti
    /// </summary>
    public abstract class TextProvider
    {
        // BRIDGE - referencia na implementáciu
        protected readonly ILanguageTextSource _languageSource;
        
        protected TextProvider(ILanguageTextSource languageSource)
        {
            _languageSource = languageSource 
                ?? throw new ArgumentNullException(nameof(languageSource));
        }
        
        /// <summary>
        /// Úroveň obtiažnosti ktorú tento provider spracováva
        /// </summary>
        public abstract DifficultyLevel DifficultyLevel { get; }
        
        /// <summary>
        /// Získaj texty vhodné pre túto úroveň obtiažnosti
        /// </summary>
        public async Task<List<Text>> GetTextsAsync(string? topic = null, int count = 10)
        {
            // Vytvor search kritériá
            var criteria = CreateSearchCriteria(topic, count);
            
            // Validácia že source podporuje túto obtiažnosť
            if (!_languageSource.SupportsDifficulty(DifficultyLevel))
            {
                throw new InvalidOperationException(
                    $"{_languageSource.LanguageName} zdroj nepodporuje úroveň {DifficultyLevel}"
                );
            }
            
            // Načítaj z implementácie (BRIDGE call!)
            var rawTexts = await _languageSource.FetchTextsAsync(criteria);
            
            // Aplikuj filtrovanie špecifické pre obtiažnosť
            var filteredTexts = ApplyDifficultyFilters(rawTexts);
            
            // Aplikuj transformácie špecifické pre obtiažnosť
            var processedTexts = ProcessTexts(filteredTexts);
            
            return processedTexts.Take(count).ToList();
        }
        
        /// <summary>
        /// Vytvor search kritériá podľa úrovne obtiažnosti
        /// </summary>
        protected abstract TextSearchCriteria CreateSearchCriteria(string? topic, int count);
        
        /// <summary>
        /// Filtruj texty podľa pravidiel obtiažnosti
        /// </summary>
        protected abstract List<Text> ApplyDifficultyFilters(List<Text> texts);
        
        /// <summary>
        /// Spracuj/transformuj texty pre úroveň obtiažnosti
        /// </summary>
        protected abstract List<Text> ProcessTexts(List<Text> texts);
        
        /// <summary>
        /// Získaj odporúčané témy pre túto úroveň
        /// </summary>
        public abstract Task<List<string>> GetRecommendedTopicsAsync();
    }
}