using System;
using System.Collections.Generic;
using System.Linq;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.BridgeAbstractions;
using POT_SEM.Services;

namespace POT_SEM.Core.Builders
{
    /// <summary>
    /// BUILDER PATTERN - Fluent API pre konštrukciu TextProvider
    /// Enhanced with caching support
    /// </summary>
    public class TextProviderBuilder
    {
        private DifficultyLevel? _difficulty;
        private string? _languageCode;
        private ILanguageTextSource? _customSource;
        
        private readonly IEnumerable<ILanguageTextSource> _languageSources;
        private readonly ITextCacheService _cache;
        
        public TextProviderBuilder(
            IEnumerable<ILanguageTextSource> languageSources,
            ITextCacheService cache)
        {
            _languageSources = languageSources 
                ?? throw new ArgumentNullException(nameof(languageSources));
            _cache = cache 
                ?? throw new ArgumentNullException(nameof(cache));
        }
        
        /// <summary>
        /// Nastav úroveň obtiažnosti
        /// </summary>
        public TextProviderBuilder ForDifficulty(DifficultyLevel level)
        {
            _difficulty = level;
            return this;
        }
        
        /// <summary>
        /// Nastav jazyk (language code: "en", "ar", "sk", "ja")
        /// </summary>
        public TextProviderBuilder ForLanguage(string languageCode)
        {
            _languageCode = languageCode?.ToLower();
            return this;
        }
        
        /// <summary>
        /// Použij vlastný language source (optional)
        /// </summary>
        public TextProviderBuilder WithSource(ILanguageTextSource source)
        {
            _customSource = source;
            return this;
        }
        
        /// <summary>
        /// Build TextProvider with cache support
        /// </summary>
        public TextProvider Build()
        {
            // Validácia
            if (!_difficulty.HasValue)
            {
                throw new InvalidOperationException(
                    "Musíš nastaviť difficulty level. Použij ForDifficulty()");
            }
            
            if (string.IsNullOrEmpty(_languageCode) && _customSource == null)
            {
                throw new InvalidOperationException(
                    "Musíš nastaviť jazyk. Použij ForLanguage() alebo WithSource()");
            }
            
            // Získaj language source
            var languageSource = _customSource ?? GetLanguageSource(_languageCode!);
            
            // Vytvor príslušný provider podľa difficulty (s cache!)
            return _difficulty.Value switch
            {
                DifficultyLevel.Beginner => new BeginnerTextProvider(languageSource, _cache),
                DifficultyLevel.Intermediate => new IntermediateTextProvider(languageSource, _cache),
                DifficultyLevel.Advanced => new AdvancedTextProvider(languageSource, _cache),
                _ => throw new ArgumentOutOfRangeException(nameof(_difficulty))
            };
        }
        
        /// <summary>
        /// Získaj language source z injected collection
        /// </summary>
        private ILanguageTextSource GetLanguageSource(string languageCode)
        {
            var source = _languageSources.FirstOrDefault(s =>
                s.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
            
            if (source == null)
            {
                var available = string.Join(", ", _languageSources.Select(s => s.LanguageCode));
                throw new InvalidOperationException(
                    $"Nenašiel sa zdroj textov pre jazyk '{languageCode}'. " +
                    $"Dostupné jazyky: {available}");
            }
            
            return source;
        }
        
        /// <summary>
        /// Získaj zoznam podporovaných jazykov
        /// </summary>
        public List<string> GetSupportedLanguages()
        {
            return _languageSources.Select(s => s.LanguageCode).ToList();
        }
        
        /// <summary>
        /// Získaj zoznam podporovaných jazykov s názvami
        /// </summary>
        public Dictionary<string, string> GetSupportedLanguagesWithNames()
        {
            return _languageSources.ToDictionary(
                s => s.LanguageCode,
                s => s.LanguageName
            );
        }
    }
}