using System.Net.Http.Json;

namespace POT_SEM.Services.Database
{
    public class SupabaseConfig
    {
        public string Url { get; set; } = "";
        public string AnonKey { get; set; } = "";
        
        /// <summary>
        /// Load Supabase configuration from appsettings.json
        /// Tries Development first, falls back to Production
        /// </summary>
        public static async Task<SupabaseConfig> LoadAsync(HttpClient http)
        {
            // Try Development config first (local development)
            try
            {
                var devConfig = await LoadFromFileAsync(http, "appsettings.Development.json");
                Console.WriteLine("✅ Using Development Supabase config");
                return devConfig;
            }
            catch (HttpRequestException)
            {
                // Development file doesn't exist, try production
                Console.WriteLine("ℹ️ Development config not found, trying production...");
            }
            
            // Fallback to production config (GitHub Pages)
            try
            {
                var prodConfig = await LoadFromFileAsync(http, "appsettings.json");
                Console.WriteLine("✅ Using Production Supabase config");
                return prodConfig;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to load Supabase config: {ex.Message}");
                throw new Exception("Supabase configuration is missing or invalid!", ex);
            }
        }
        
        private static async Task<SupabaseConfig> LoadFromFileAsync(HttpClient http, string fileName)
        {
            var response = await http.GetAsync(fileName);
            response.EnsureSuccessStatusCode();
            
            var settings = await response.Content.ReadFromJsonAsync<AppSettings>();
            
            if (settings?.Supabase == null)
            {
                throw new Exception($"Supabase section not found in {fileName}");
            }
            
            var config = settings.Supabase;
            
            // Validate
            if (string.IsNullOrWhiteSpace(config.Url) || config.Url.Contains("PLACEHOLDER"))
            {
                throw new Exception($"Invalid Supabase URL in {fileName}");
            }
            
            if (string.IsNullOrWhiteSpace(config.AnonKey) || config.AnonKey.Contains("PLACEHOLDER"))
            {
                throw new Exception($"Invalid Supabase AnonKey in {fileName}");
            }
            
            return config;
        }
    }
    
    internal class AppSettings
    {
        public SupabaseConfig Supabase { get; set; } = new();
    }
}