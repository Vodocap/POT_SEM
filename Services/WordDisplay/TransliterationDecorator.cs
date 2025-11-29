using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.WordDisplay
{
    /// <summary>
    /// DECORATOR - Adds transliteration (Arabic: كتاب → kitab)
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
        
        public string GetTooltipText()
        {
            var baseTooltip = _inner.GetTooltipText();
            return !string.IsNullOrEmpty(_transliteration)
                ? $"{baseTooltip} ({_transliteration})"
                : baseTooltip;
        }
        
        public string GetCssClass() => _inner.GetCssClass() + " has-transliteration";
    }
}