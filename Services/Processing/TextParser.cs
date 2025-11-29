using POT_SEM.Core.Models;
using System.Text.RegularExpressions;

namespace POT_SEM.Services.Processing
{
    /// <summary>
    /// COMPOSITE PATTERN - Text parser
    /// Breaks text into hierarchical structure: Text → Sentences → Words
    /// </summary>
    public class TextParser
    {
        /// <summary>
        /// Parse text into structured format
        /// </summary>
        public ProcessedText ParseText(Text originalText, string sourceLang, string targetLang)
        {
            Console.WriteLine($"📝 Parsing text ({sourceLang}): {originalText.Content.Length} chars");
            
            var content = originalText.Content;
            var sentences = new List<ProcessedSentence>();
            
            // Split by sentence delimiters (language-aware)
            var sentenceTexts = SplitIntoSentences(content, sourceLang);
            
            for (int i = 0; i < sentenceTexts.Count; i++)
            {
                var sentence = ParseSentence(sentenceTexts[i], i, sourceLang);
                sentences.Add(sentence);
            }
            
            var processedText = new ProcessedText
            {
                OriginalText = originalText,
                SourceLanguage = sourceLang,
                TargetLanguage = targetLang,
                Sentences = sentences
            };
            
            Console.WriteLine($"   ✅ Parsed: {processedText.TotalSentences} sentences, {processedText.TotalWords} words");
            Console.WriteLine($"   Unique words to translate: {processedText.UniqueWords}");
            
            return processedText;
        }
        
        /// <summary>
        /// Split text into sentences (language-aware)
        /// </summary>
        private List<string> SplitIntoSentences(string text, string language)
        {
            // Japanese uses different punctuation
            if (language == "ja")
            {
                return Regex.Split(text, @"(?<=[。！？])")
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            
            // Arabic uses different punctuation
            if (language == "ar")
            {
                return Regex.Split(text, @"(?<=[.!?؟])\s+")
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            
            // English, Slovak, etc.
            return Regex.Split(text, @"(?<=[.!?])\s+")
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
        
        /// <summary>
        /// Parse single sentence into words (language-aware)
        /// </summary>
        private ProcessedSentence ParseSentence(string sentenceText, int sentenceIndex, string language)
        {
            List<ProcessedWord> words;
            
            // Language-specific parsing
            if (language == "ja")
            {
                words = ParseJapaneseSentence(sentenceText);
            }
            else if (language == "ar")
            {
                words = ParseArabicSentence(sentenceText);
            }
            else
            {
                words = ParseSpaceSeparatedSentence(sentenceText);
            }
            
            return new ProcessedSentence
            {
                OriginalText = sentenceText,
                Words = words,
                Index = sentenceIndex
            };
        }
        
        /// <summary>
        /// Parse Japanese (no spaces, mixed scripts)
        /// </summary>
        private List<ProcessedWord> ParseJapaneseSentence(string sentence)
        {
            var words = new List<ProcessedWord>();
            int position = 0;
            
            // Simple tokenization: split on script changes and punctuation
            var currentWord = "";
            var currentType = GetJapaneseCharType(sentence.FirstOrDefault());
            
            foreach (var ch in sentence)
            {
                var charType = GetJapaneseCharType(ch);
                
                // Same type: continue word
                if (charType == currentType && charType != CharType.Punctuation)
                {
                    currentWord += ch;
                }
                // Different type or punctuation: end word
                else
                {
                    if (!string.IsNullOrEmpty(currentWord))
                    {
                        words.Add(CreateWord(currentWord, words.Count, position++, currentType == CharType.Punctuation));
                    }
                    
                    currentWord = ch.ToString();
                    currentType = charType;
                }
            }
            
            // Add last word
            if (!string.IsNullOrEmpty(currentWord))
            {
                words.Add(CreateWord(currentWord, words.Count, position, currentType == CharType.Punctuation));
            }
            
            return words;
        }
        
        /// <summary>
        /// Parse Arabic (RTL language)
        /// </summary>
        private List<ProcessedWord> ParseArabicSentence(string sentence)
        {
            var words = new List<ProcessedWord>();
            
            // Arabic uses spaces like English
            var tokens = Regex.Split(sentence, @"(\s+|[،؛,;.!?؟])")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            
            int position = 0;
            
            foreach (var token in tokens)
            {
                var isPunctuation = Regex.IsMatch(token, @"^[،؛,;.!?؟]+$");
                words.Add(CreateWord(token, words.Count, position++, isPunctuation));
            }
            
            return words;
        }
        
        /// <summary>
        /// Parse space-separated languages (English, Slovak)
        /// </summary>
        private List<ProcessedWord> ParseSpaceSeparatedSentence(string sentence)
        {
            var words = new List<ProcessedWord>();
            
            // Split on spaces and punctuation, but keep punctuation
            var tokens = Regex.Split(sentence, @"(\s+|[,;:.!?])")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            
            int position = 0;
            
            foreach (var token in tokens)
            {
                var isPunctuation = Regex.IsMatch(token, @"^[,;:.!?]+$");
                words.Add(CreateWord(token, words.Count, position++, isPunctuation));
            }
            
            return words;
        }
        
        /// <summary>
        /// Create ProcessedWord instance
        /// </summary>
        private ProcessedWord CreateWord(string text, int index, int position, bool isPunctuation)
        {
            return new ProcessedWord
            {
                Original = text,
                Normalized = text.ToLower().Trim(),
                Index = index,
                PositionInSentence = position,
                IsPunctuation = isPunctuation
            };
        }
        
        /// <summary>
        /// Determine Japanese character type
        /// </summary>
        private CharType GetJapaneseCharType(char ch)
        {
            if (char.IsPunctuation(ch) || "、。！？".Contains(ch))
            {
                return CharType.Punctuation;
            }
            
            // Hiragana
            if (ch >= '\u3040' && ch <= '\u309F')
            {
                return CharType.Hiragana;
            }
            
            // Katakana
            if (ch >= '\u30A0' && ch <= '\u30FF')
            {
                return CharType.Katakana;
            }
            
            // Kanji
            if (ch >= '\u4E00' && ch <= '\u9FFF')
            {
                return CharType.Kanji;
            }
            
            return CharType.Other;
        }
        
        /// <summary>
        /// Extract unique words from processed text (for translation)
        /// </summary>
        public List<string> ExtractUniqueWords(ProcessedText processedText)
        {
            return processedText.Sentences
                .SelectMany(s => s.Words)
                .Where(w => !w.IsPunctuation)
                .Select(w => w.Normalized)
                .Distinct()
                .ToList();
        }
        
        /// <summary>
        /// Character type enum for Japanese parsing
        /// </summary>
        private enum CharType
        {
            Hiragana,
            Katakana,
            Kanji,
            Punctuation,
            Other
        }
    }
}