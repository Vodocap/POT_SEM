using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Processing
{
    /// <summary>
    /// FACADE PATTERN - Simplified interface for text processing pipeline
    /// Coordinates: Parsing → Translation → Enhancement
    /// </summary>
    public class TextProcessingFacade
    {
        private readonly TextParser _parser;
        private readonly ITranslationStrategy _translationChain;
        private readonly POT_SEM.Services.Transliteration.FuriganaEnrichmentService? _furiganaEnrichment;
        private readonly IEnumerable<POT_SEM.Core.Interfaces.ITransliterationService> _transliterationServices;
        private readonly POT_SEM.Services.Translation.TranslationFlyweightFactory? _flyweight;

        public TextProcessingFacade(
            TextParser parser,
            ITranslationStrategy translationChain,
            IEnumerable<POT_SEM.Core.Interfaces.ITransliterationService> transliterationServices,
            POT_SEM.Services.Translation.TranslationFlyweightFactory? flyweight = null,
            POT_SEM.Services.Transliteration.FuriganaEnrichmentService? furiganaEnrichment = null)
        {
            _parser = parser;
            _translationChain = translationChain;
            _transliterationServices = transliterationServices ?? Enumerable.Empty<POT_SEM.Core.Interfaces.ITransliterationService>();
            _flyweight = flyweight;
            _furiganaEnrichment = furiganaEnrichment;
        }
        
        /// <summary>
        /// Process entire text: parse, translate, enhance
        /// </summary>
        public async Task<ProcessedText> ProcessTextAsync(
            Text originalText, 
            string sourceLang, 
            string targetLang)
        {
            Console.WriteLine($"🎭 FACADE: Processing text ({sourceLang} → {targetLang})");
            Console.WriteLine($"   Title: {originalText.Title}");
            Console.WriteLine($"   Length: {originalText.Content.Length} chars");
            
            // 1. Parse text into structured format
            var processedText = _parser.ParseText(originalText, sourceLang, targetLang);
            
            // 2. Extract unique words for translation
            var uniqueWords = _parser.ExtractUniqueWords(processedText);
            
            Console.WriteLine($"   Unique words to translate: {uniqueWords.Count}");
            
            // 3. Translate all unique words (CHAIN will handle caching + dictionary + DB + API)
            var translations = await _translationChain.TranslateBatchAsync(
                uniqueWords, 
                sourceLang, 
                targetLang);
            
            // 4. Apply translations to words
            foreach (var sentence in processedText.Sentences)
            {
                foreach (var word in sentence.Words)
                {
                    if (!word.IsPunctuation && translations.ContainsKey(word.Normalized))
                    {
                        word.Translation = translations[word.Normalized];
                    }
                }
            }
        

        

            // Apply furigana decorator (if available and language is Japanese)
            if (_furiganaEnrichment != null)
            {
                processedText = await _furiganaEnrichment.EnrichTextAsync(processedText);
            }

            // 4.b Generate transliterations when available (e.g., Arabic, Japanese)
            foreach (var sentence in processedText.Sentences)
            {
                foreach (var word in sentence.Words)
                {
                    if (word.IsPunctuation) continue;

                    // Check flyweight cache first
                    var cached = _flyweight?.GetTransliteration(processedText.SourceLanguage, word.Normalized);
                if (!string.IsNullOrEmpty(cached))
                    {
                        word.Transliteration = cached;
                        continue;
                    }

                    // Find a transliteration service that supports the source language
                    var svc = _transliterationServices.FirstOrDefault(s => s.SupportsLanguage(processedText.SourceLanguage));
                    if (svc == null) continue;

                    try
                    {
                        // If transliteration is already present (e.g., set by JS decorator), keep it
                        if (!string.IsNullOrEmpty(word.Transliteration))
                        {
                            Console.WriteLine($"[Transliteration] Skipping, already set for '{word.Original}' => '{word.Transliteration}'");
                            continue;
                        }

                        // For Japanese prefer to transliterate from furigana (hiragana) when available
                        var input = word.Furigana ?? word.Original;
                        var t = await svc.TransliterateAsync(input, processedText.SourceLanguage);
                        if (!string.IsNullOrEmpty(t))
                        {
                            word.Transliteration = t;
                            Console.WriteLine($"[Transliteration] Set for '{word.Original}' => '{t}'");
                            try { _flyweight?.AddTransliteration(processedText.SourceLanguage, word.Normalized, t); } catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Transliteration failed for '{word.Original}': {ex.Message}");
                    }
                }
            }

            // 5. Translate sentences (preserve context) using translation strategy
            foreach (var sentence in processedText.Sentences)
            {
                try
                {
                    var sentTrans = await _translationChain.TranslateSentenceAsync(sentence.OriginalText, sourceLang, targetLang);
                    if (!string.IsNullOrEmpty(sentTrans)) sentence.Translation = sentTrans;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️ Sentence translation failed: {ex.Message}");
                }
            }
            
            // (moved furigana/transliteration earlier)

            Console.WriteLine($"   ✅ FACADE complete: {processedText.TotalSentences} sentences, {processedText.TotalWords} words");

            return processedText;
        }
        
        /// <summary>
        /// Process single sentence
        /// </summary>
        public async Task<ProcessedSentence> ProcessSentenceAsync(
            string sentenceText, 
            string sourceLang, 
            string targetLang)
        {
            // Create a temporary Text object
            var tempText = new Text
            {
                Content = sentenceText,
                Language = sourceLang,
                Difficulty = DifficultyLevel.Intermediate
            };
            
            var processedText = _parser.ParseText(tempText, sourceLang, targetLang);
            var sentence = processedText.Sentences.FirstOrDefault();
            
            if (sentence == null)
            {
                return new ProcessedSentence
                {
                    OriginalText = sentenceText,
                    Words = new List<ProcessedWord>(),
                    Index = 0
                };
            }
            
            // Get unique words
            var uniqueWords = sentence.Words
                .Where(w => !w.IsPunctuation)
                .Select(w => w.Normalized)
                .Distinct()
                .ToList();
            
            // Translate
            var translations = await _translationChain.TranslateBatchAsync(
                uniqueWords, 
                sourceLang, 
                targetLang);
            
            // Apply translations
            foreach (var word in sentence.Words)
            {
                if (!word.IsPunctuation && translations.ContainsKey(word.Normalized))
                {
                    word.Translation = translations[word.Normalized];
                }
            }

            // Apply furigana decorators (if available)
            if (_furiganaEnrichment != null)
            {
                var temp = new ProcessedText { OriginalText = tempText, SourceLanguage = sourceLang, TargetLanguage = targetLang, Sentences = new List<ProcessedSentence> { sentence } };
                await _furiganaEnrichment.EnrichTextAsync(temp);
                // copy back furigana into sentence (decorator modifies in place)
                sentence = temp.Sentences.First();
            }

            // Generate transliteration using furigana when possible
            var svc = _transliterationServices.FirstOrDefault(s => s.SupportsLanguage(sourceLang));
            if (svc != null)
            {
                foreach (var word in sentence.Words)
                {
                    if (word.IsPunctuation) continue;
                    var cached = _flyweight?.GetTransliteration(sourceLang, word.Normalized);
                    if (!string.IsNullOrEmpty(cached))
                    {
                        word.Transliteration = cached;
                        continue;
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(word.Transliteration))
                        {
                            Console.WriteLine($"[Transliteration] (sentence) Skipping, already set for '{word.Original}' => '{word.Transliteration}'");
                            continue;
                        }

                        var input = word.Furigana ?? word.Original;
                        var t = await svc.TransliterateAsync(input, sourceLang);
                        if (!string.IsNullOrEmpty(t))
                        {
                            word.Transliteration = t;
                            Console.WriteLine($"[Transliteration] (sentence) Set for '{word.Original}' => '{t}'");
                            try { _flyweight?.AddTransliteration(sourceLang, word.Normalized, t); } catch { }
                        }
                    }
                    catch { }
                }
            }

            // Translate sentence (preserve context) using translation strategy
            try
            {
                var sentTrans = await _translationChain.TranslateSentenceAsync(sentence.OriginalText, sourceLang, targetLang);
                if (!string.IsNullOrEmpty(sentTrans)) sentence.Translation = sentTrans;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Sentence translation failed: {ex.Message}");
            }

            return sentence;
        }
    }
}