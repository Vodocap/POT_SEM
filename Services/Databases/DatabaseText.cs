using System.Text.Json.Serialization;
using Postgrest.Attributes;
using Postgrest.Models;

namespace POT_SEM.Services.Databases
{
    [Table("texts")]
    public class DatabaseText : BaseModel
    {
        [PrimaryKey("id", false)]
        [JsonPropertyName("id")]
        public long Id { get; set; }
        
        [Column("language_code")]
        [JsonPropertyName("language_code")]
        public string LanguageCode { get; set; } = "";
        
        [Column("difficulty")]
        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "";
        
        [Column("title")]
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";
        
        [Column("content")]
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
        
        [Column("topic")]
        [JsonPropertyName("topic")]
        public string? Topic { get; set; }
        
        [Column("word_count")]
        [JsonPropertyName("word_count")]
        public int WordCount { get; set; }
        
        [Column("created_at")]
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}