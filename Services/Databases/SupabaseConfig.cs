using System.Net.Http.Json;

namespace POT_SEM.Services.Databases
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
                return devConfig;
            }
            catch (HttpRequestException)
            {
                // Development file doesn't exist, try production
            }
            
            // Fallback to production config (GitHub Pages)
            try
            {
                var prodConfig = await LoadFromFileAsync(http, "appsettings.json");
                return prodConfig;
            }
            catch (Exception ex)
            {
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