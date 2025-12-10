using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// Adds transliteration layer for Arabic words
    /// </summary>
    public class TransliterationDecorator : IWordDisplay
    {
        private readonly IWordDisplay _inner;
        private readonly string? _transliteration;
        
        public TransliterationDecorator(IWordDisplay inner, string? transliteration)
        {
            _inner = inner;
            _transliteration = transliteration;
        }
        
        public List<DisplayLayer> GetLayers()
        {
            var layers = _inner.GetLayers();
            
            if (!string.IsNullOrEmpty(_transliteration))
            {
                layers.Add(new DisplayLayer
                {
                    Type = "transliteration",
                    Text = _transliteration,
                    CssClass = "word-transliteration",
                    Order = 10
                });
            }
            
            return layers;
        }
        
        public string GetDisplayText() => _inner.GetDisplayText();
        
        public string GetCssClass() => _inner.GetCssClass() + " has-transliteration";
    }
}