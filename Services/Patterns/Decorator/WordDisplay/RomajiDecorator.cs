using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// Adds romaji (latin transliteration) layer for Japanese words
    /// </summary>
    public class RomajiDecorator : IWordDisplay
    {
        private readonly IWordDisplay _inner;
        private readonly string? _romaji;
        
        public RomajiDecorator(IWordDisplay inner, string? romaji)
        {
            _inner = inner;
            _romaji = romaji;
        }
        
        public List<DisplayLayer> GetLayers()
        {
            var layers = _inner.GetLayers();
            
            if (!string.IsNullOrEmpty(_romaji))
            {
                layers.Add(new DisplayLayer
                {
                    Type = "romaji",
                    Text = _romaji,
                    CssClass = "word-romaji",
                    Order = 15
                });
            }
            
            return layers;
        }
        
        public string GetDisplayText() => _inner.GetDisplayText();
        
        public string GetCssClass() => _inner.GetCssClass() + " has-romaji";
    }
}