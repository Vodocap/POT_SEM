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
        private readonly POT_SEM.Services.Decorators.FuriganaDecorator? _furiganaDecorator;
        private readonly POT_SEM.Services.Decorators.HeuristicFuriganaDecorator? _heuristicDecorator;

        public TextProcessingFacade(
            TextParser parser,
            ITranslationStrategy translationChain,
            POT_SEM.Services.Decorators.FuriganaDecorator? furiganaDecorator = null,
            POT_SEM.Services.Decorators.HeuristicFuriganaDecorator? heuristicDecorator = null)
        {
            _parser = parser;
            _translationChain = translationChain;
            _furiganaDecorator = furiganaDecorator;
            _heuristicDecorator = heuristicDecorator;
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
            
            // 3. Translate all unique words (CHAIN will handle caching + DB + API)
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
            
            // 5. Apply furigana decorator (if available and language is Japanese)
            if (_furiganaDecorator != null)
            {
                processedText = await _furiganaDecorator.DecorateTextAsync(processedText);
            }

            // 6. If words still lack furigana, try the heuristic decorator as a fallback
            if (_heuristicDecorator != null)
            {
                await _heuristicDecorator.DecorateTextAsync(processedText);
            }

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