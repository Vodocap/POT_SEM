using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// Adds translation layer
    /// </summary>
    public class TranslationDecorator : IWordDisplay
    {
        private readonly IWordDisplay _inner;
        private readonly string? _translation;
        
        public TranslationDecorator(IWordDisplay inner, string? translation)
        {
            _inner = inner;
            _translation = translation;
        }
        
        public List<DisplayLayer> GetLayers()
        {
            var layers = _inner.GetLayers();
            
            if (!string.IsNullOrEmpty(_translation))
            {
                layers.Add(new DisplayLayer
                {
                    Type = "translation",
                    Text = _translation,
                    CssClass = "word-translation",
                    Order = 100
                });
            }
            
            return layers;
        }
        
        public string GetDisplayText() => _inner.GetDisplayText();
        
        public string GetCssClass() => _inner.GetCssClass() + " has-translation";
    }
}