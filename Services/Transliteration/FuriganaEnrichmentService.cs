using POT_SEM.Core.Models;
using POT_SEM.Core.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace POT_SEM.Services.Transliteration
{
    /// <summary>
    /// Furigana enrichment service with API integration
    /// Enriches ProcessedText with furigana and transliteration data
    /// 1. Applies static dictionary for common words
    /// 2. Calls Fungana AI API for kanji words
    /// 3. Generates romaji transliteration for all Japanese words
    /// </summary>
    public class FuriganaEnrichmentService : ITransliterationService
    {
        private readonly HttpClient? _httpClient;
        private readonly JapaneseRomajiService? _romajiService;
        private const string FUNGANA_API_URL = "https://furigana-ai.matusmonogram.workers.dev/";
        
        public string ServiceName => "Japanese Furigana & Romaji Enrichment";
        
        private static readonly Dictionary<string, string> _staticReadings = new()
        {
            ["東京"] = "とうきょう",
            ["日本"] = "にほん",
            ["学生"] = "がくせい",
            ["湯倉神社"] = "ゆくらじんじゃ",
            ["東郷村"] = "とうごうむら",
            ["カール・レムリ"] = "かーる・れむり"
        };

        public FuriganaEnrichmentService()
        {
        }

        public FuriganaEnrichmentService(HttpClient httpClient, JapaneseRomajiService romajiService)
        {
            _httpClient = httpClient;
            _romajiService = romajiService;
        }

        public bool SupportsLanguage(string language) => language == "ja";

        public async Task<string?> TransliterateAsync(string text, string language)
        {
            if (language != "ja") return null;
            
            // For single word transliteration, try to get furigana first then convert
            if (_httpClient != null && HasKanji(text))
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync(FUNGANA_API_URL, new { text });
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<FunganaResponse>();
                        if (result?.Hiragana != null && _romajiService != null)
                        {
                            return await _romajiService.TransliterateAsync(result.Hiragana, language);
                        }
                    }
                }
                catch
                {
                    // Fall through to romaji service
                }
            }

            // Use romaji service directly for hiragana/katakana
            if (_romajiService != null)
            {
                return await _romajiService.TransliterateAsync(text, language);
            }

            return null;
        }

        private static bool HasKanji(string text)
        {
            return text.Any(c => c >= '\u4E00' && c <= '\u9FFF');
        }

        public async Task<ProcessedText> EnrichTextAsync(ProcessedText text)
        {
            if (text == null) return null!;
            if (text.SourceLanguage?.ToLower() != "ja")
            {
                return text;
            }

            foreach (var sentence in text.Sentences)
            {
                foreach (var word in sentence.Words)
                {
                    if (word.IsPunctuation)
                    {
                        continue;
                    }

                    // Step 1: Try static dictionary first
                    if (_staticReadings.TryGetValue(word.Original, out var reading))
                    {
                        word.Furigana = reading;
                        word.Metadata["hasFurigana"] = true;
                    }

                    // Check if word contains Kanji characters
                    bool hasKanji = word.Original.Any(c => c >= '\u4E00' && c <= '\u9FFF');

                    // Step 2: If word has kanji and no furigana, call API (if available)
                    if (hasKanji && string.IsNullOrEmpty(word.Furigana) && _httpClient != null)
                    {
                        try
                        {
                            var response = await _httpClient.PostAsJsonAsync(FUNGANA_API_URL, new { text = word.Original });
                            
                            if (response.IsSuccessStatusCode)
                            {
                                var result = await response.Content.ReadFromJsonAsync<FunganaResponse>();
                                
                                if (result?.Hiragana != null)
                                {
                                    var hiragana = result.Hiragana;
                                    
                                    word.Furigana = hiragana;
                                    word.Metadata["hasFurigana"] = true;
                                }
                            }
                        }
                        catch
                        {
                            // Fungana API call failed
                        }
                    }

                    // Step 3: Generate romaji transliteration (if service available)
                    if (_romajiService != null && string.IsNullOrEmpty(word.Transliteration))
                    {
                        try
                        {
                            // For kanji words with furigana, transliterate the furigana
                            // For hiragana-only words, transliterate the word itself
                            var input = !string.IsNullOrEmpty(word.Furigana) ? word.Furigana : word.Original;
                            
                            var romaji = await _romajiService.TransliterateAsync(input, "ja");
                            if (!string.IsNullOrEmpty(romaji))
                            {
                                word.Transliteration = romaji;
                            }
                        }
                        catch
                        {
                            // Transliteration failed
                        }
                    }
                }
            }

            return text;
        }

        private class FunganaResponse
        {
            [JsonPropertyName("kanji")]
            public string? Kanji { get; set; }

            [JsonPropertyName("hiragana")]
            public string? Hiragana { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }
    }
}
