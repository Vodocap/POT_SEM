using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;
using POT_SEM.Services.Patterns.Flyweight;
using POT_SEM.Services.TextProviders.Parsing;

namespace POT_SEM.Services.Patterns.Facade
{
    /// <summary>
    /// Simplified interface for text processing: Parsing -> Translation -> Enhancement
    /// </summary>
    public class TextProcessingFacade
    {
        private readonly ITranslationStrategy _translationChain;
        private readonly POT_SEM.Services.Transliteration.FuriganaEnrichmentService? _furiganaEnrichment;
        private readonly IEnumerable<POT_SEM.Core.Interfaces.ITransliterationService> _transliterationServices;
        
        public event Action<string>?OnProgress;

        public TextProcessingFacade(
            ITranslationStrategy translationChain,
            IEnumerable<POT_SEM.Core.Interfaces.ITransliterationService> transliterationServices,
            POT_SEM.Services.Transliteration.FuriganaEnrichmentService? furiganaEnrichment = null)
        {
            _translationChain = translationChain;
            _transliterationServices = transliterationServices ?? Enumerable.Empty<POT_SEM.Core.Interfaces.ITransliterationService>();
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
            Console.WriteLine($"Starting text processing: {sourceLang} -> {targetLang}");
            OnProgress?.Invoke("Parsing text structure...");
            
            var parser = LanguageParserFactory.CreateParser(sourceLang);
            var processedText = parser.ParseText(originalText, targetLang);
            Console.WriteLine($"Parsed {processedText.TotalSentences} sentences, {processedText.TotalWords} words");
            
            Console.WriteLine($"Skipping batch word translation (on-demand loading)");  

            if (_furiganaEnrichment != null)
            {
                processedText = await _furiganaEnrichment.EnrichTextAsync(processedText);
            }

            var transliteratedCount = 0;
            foreach (var sentence in processedText.Sentences)
            {
                foreach (var word in sentence.Words)
                {
                    if (word.IsPunctuation) continue;

                    var svc = _transliterationServices.FirstOrDefault(s => s.SupportsLanguage(processedText.SourceLanguage));
                    if (svc == null) continue;

                    try
                    {
                        if (!string.IsNullOrEmpty(word.Transliteration))
                        {
                            continue;
                        }

                        var input = word.Furigana ?? word.Original;
                        var t = await svc.TransliterateAsync(input, processedText.SourceLanguage);
                        if (!string.IsNullOrEmpty(t))
                        {
                            word.Transliteration = t;
                            transliteratedCount++;
                        }
                    }
                    catch
                    {
                        // Transliteration failed, continue
                    }
                }
            }
            Console.WriteLine($"Transliterated {transliteratedCount} words");

            Console.WriteLine($"Translating {processedText.TotalSentences} sentences...");
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
            Console.WriteLine($"Translated {sentenceTranslatedCount}/{processedText.TotalSentences} sentences");
            
            // (moved furigana/transliteration earlier)
            Console.WriteLine("Text processing complete!");
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
            
            var parser = LanguageParserFactory.CreateParser(sourceLang);
            var processedText = parser.ParseText(tempText, targetLang);
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
                        }
                    }
                    catch { }
                }
            }
            try
            {
                var sentTrans = await _translationChain.TranslateSentenceAsync(sentence.OriginalText, sourceLang, targetLang);
                if (!string.IsNullOrEmpty(sentTrans)) sentence.Translation = sentTrans;
            }
            catch
            {
            }

            return sentence;
        }
        
        /// <summary>
        /// Translate single word
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