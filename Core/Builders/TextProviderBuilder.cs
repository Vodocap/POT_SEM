using System;
using System.Collections.Generic;
using System.Linq;
using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using POT_SEM.Core.BridgeAbstractions;

namespace POT_SEM.Core.Builders
{
    /// <summary>
    /// BUILDER PATTERN - Fluent API pre konštrukciu TextProvider
    /// </summary>
    public class TextProviderBuilder
    {
        private DifficultyLevel? _difficulty;
        private string? _languageCode;
        private ILanguageTextSource? _customSource;
        private readonly IServiceProvider _serviceProvider;
        
        public TextProviderBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
        /// Nastav jazyk (language code: "en", "ar", "sk", "ru", "ja")
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
        /// Build TextProvider
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
            
            // Získaj language source z DI alebo použij custom
            var languageSource = _customSource ?? GetLanguageSource(_languageCode!);
            
            // Vytvor príslušný provider podľa difficulty
            return _difficulty.Value switch
            {
                DifficultyLevel.Beginner => new BeginnerTextProvider(languageSource),
                DifficultyLevel.Intermediate => new IntermediateTextProvider(languageSource),
                DifficultyLevel.Advanced => new AdvancedTextProvider(languageSource),
                _ => throw new ArgumentOutOfRangeException(nameof(_difficulty))
            };
        }
        
        /// <summary>
        /// Získaj language source z DI container
        /// </summary>
        private ILanguageTextSource GetLanguageSource(string languageCode)
        {
            var sources = (IEnumerable<ILanguageTextSource>?)_serviceProvider
                .GetService(typeof(IEnumerable<ILanguageTextSource>))
                ?? Enumerable.Empty<ILanguageTextSource>();

            var source = sources.FirstOrDefault(s =>
                s.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
            
            if (source == null)
            {
                var available = string.Join(", ", sources.Select(s => s.LanguageCode));
                throw new InvalidOperationException(
                    $"Nenašiel sa zdroj textov pre jazyk '{languageCode}'. " +
                    $"Dostupné jazyky: {available}");
            }
            
            return source;
        }
    }
}