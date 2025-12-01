using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.Composite;
using POT_SEM.Services.Patterns.Flyweight;

namespace POT_SEM.Services.Patterns.Facade
{
    /// <summary>
    /// FACADE PATTERN - Simplified interface for text processing pipeline
    /// Coordinates: Parsing → Translation → Enhancement
    /// </summary>
    public class TextProcessingFacade
    {
        private readonly ITranslationStrategy _translationChain;
        private readonly POT_SEM.Services.Transliteration.FuriganaEnrichmentService? _furiganaEnrichment;
        private readonly IEnumerable<POT_SEM.Core.Interfaces.ITransliterationService> _transliterationServices;
        private readonly TranslationCacheService? _cache;
        
        public event Action<string>?OnProgress;

        public TextProcessingFacade(
            ITranslationStrategy translationChain,
            IEnumerable<POT_SEM.Core.Interfaces.ITransliterationService> transliterationServices,
            TranslationCacheService? cache = null,
            POT_SEM.Services.Transliteration.FuriganaEnrichmentService? furiganaEnrichment = null)
        {
            _translationChain = translationChain;
            _transliterationServices = transliterationServices ?? Enumerable.Empty<POT_SEM.Core.Interfaces.ITransliterationService>();
            _cache = cache;
            _furiganaEnrichment = furiganaEnrichment;
        }
        
        /// <summary>
        /// Process entire text: parse, translate sentences and transliteration only (words on-demand)
        /// </summary>
        public async Task<ProcessedText> ProcessTextAsync(
            Text originalText, 
            string sourceLang, 
            string targetLang)
        {
            Console.WriteLine($"📖 Starting text processing: {sourceLang} → {targetLang}");
            OnProgress?.Invoke("Parsing text structure...");
            
            // 1. Parse text into structured format using language-specific strategy
            var parser = TextParserFactory.CreateParser(sourceLang);
            var processedText = parser.ParseText(originalText, sourceLang, targetLang);
            Console.WriteLine($"✅ Parsed {processedText.TotalSentences} sentences, {processedText.TotalWords} words");
            
            // 2. SKIP word batch translation - words will be translated on-demand in UI
            Console.WriteLine($"⏭️ Skipping batch word translation (on-demand loading)");
        

        

            // Apply furigana decorator (if available and language is Japanese)
            if (_furiganaEnrichment != null)
            {
                Console.WriteLine("🎌 Enriching Japanese text with furigana...");
                OnProgress?.Invoke("Adding furigana readings...");
                processedText = await _furiganaEnrichment.EnrichTextAsync(processedText);
                Console.WriteLine("✅ Furigana enrichment complete");
            }

            // 4.b Generate transliterations when available (e.g., Arabic, Japanese)
            Console.WriteLine("🔄 Generating transliterations...");
            OnProgress?.Invoke("Generating transliterations...");
            var transliteratedCount = 0;
            foreach (var sentence in processedText.Sentences)
            {
                foreach (var word in sentence.Words)
                {
                    if (word.IsPunctuation) continue;

                    // Check flyweight cache first
                    var cached = _cache?.GetTransliteration(processedText.SourceLanguage, word.Normalized);
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
                            continue;
                        }

                        // For Japanese prefer to transliterate from furigana (hiragana) when available
                        var input = word.Furigana ?? word.Original;
                        var t = await svc.TransliterateAsync(input, processedText.SourceLanguage);
                        if (!string.IsNullOrEmpty(t))
                        {
                            word.Transliteration = t;
                            transliteratedCount++;
                            try { _cache?.AddTransliteration(processedText.SourceLanguage, word.Normalized, t); } catch { }
                        }
                    }
                    catch
                    {
                        // Transliteration failed, continue
                    }
                }
            }
            Console.WriteLine($"✅ Transliterated {transliteratedCount} words");

            // 4. Translate sentences (preserve context) using translation strategy
            Console.WriteLine($"📝 Translating {processedText.TotalSentences} sentences...");
            OnProgress?.Invoke($"Translating {processedText.TotalSentences} sentences...");
            var sentenceTranslatedCount = 0;
            foreach (var sentence in processedText.Sentences)
            {
                try
                {
                    var sentTrans = await _translationChain.TranslateSentenceAsync(sentence.OriginalText, sourceLang, targetLang);
                    if (!string.IsNullOrEmpty(sentTrans))
                    {
                        sentence.Translation = sentTrans;
                        sentenceTranslatedCount++;
                    }
                }
                catch
                {
                    // Sentence translation failed, continue
                }
            }
            Console.WriteLine($"✅ Translated {sentenceTranslatedCount}/{processedText.TotalSentences} sentences");
            
            // (moved furigana/transliteration earlier)
            Console.WriteLine("🎉 Text processing complete!");
            OnProgress?.Invoke("Processing complete!");

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
            
            var parser = TextParserFactory.CreateParser(sourceLang);
            var processedText = parser.ParseText(tempText, sourceLang, targetLang);
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
                    var cached = _cache?.GetTransliteration(sourceLang, word.Normalized);
                    if (!string.IsNullOrEmpty(cached))
                    {
                        word.Transliteration = cached;
                        continue;
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(word.Transliteration))
                        {
                            continue;
                        }

                        var input = word.Furigana ?? word.Original;
                        var t = await svc.TransliterateAsync(input, sourceLang);
                        if (!string.IsNullOrEmpty(t))
                        {
                            word.Transliteration = t;
                            try { _cache?.AddTransliteration(sourceLang, word.Normalized, t); } catch { }
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
            catch
            {
                // Sentence translation failed, continue
            }

            return sentence;
        }
        
        /// <summary>
        /// Translate single word (on-demand)
        /// </summary>
        public async Task<string?> TranslateWordAsync(
            string word,
            string sourceLang,
            string targetLang)
        {
            try
            {
                var normalized = word.ToLower().Trim();
                var result = await _translationChain.TranslateWordAsync(normalized, sourceLang, targetLang);
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}