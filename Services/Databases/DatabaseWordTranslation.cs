using Postgrest.Attributes;
using Postgrest.Models;
using System.Text.Json.Serialization;

namespace POT_SEM.Services.Database
{
    /// <summary>
    /// Database model for word translations (Supabase table: word_translations)
    /// </summary>
    [Table("word_translations")]
    public class DatabaseWordTranslation : BaseModel
    {
        [PrimaryKey("id", false)]
        [JsonPropertyName("id")]
        public long Id { get; set; }  // ← ZMENA: int → long
        
        [Column("source_lang")]
        [JsonPropertyName("source_lang")]
        public string SourceLang { get; set; } = string.Empty;
        
        [Column("target_lang")]
        [JsonPropertyName("target_lang")]
        public string TargetLang { get; set; } = string.Empty;
        
        [Column("original_word")]
        [JsonPropertyName("original_word")]
        public string OriginalWord { get; set; } = string.Empty;
        
        [Column("translation")]
        [JsonPropertyName("translation")]
        public string Translation { get; set; } = string.Empty;
        
        [Column("transliteration")]
        [JsonPropertyName("transliteration")]
        public string? Transliteration { get; set; }
        
        [Column("furigana")]
        [JsonPropertyName("furigana")]
        public string? Furigana { get; set; }
        
        [Column("usage_count")]
        [JsonPropertyName("usage_count")]
        public int UsageCount { get; set; }
        
        [Column("created_at")]
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}