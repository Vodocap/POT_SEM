using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.WordDisplay
{
    /// <summary>
    /// DECORATOR - Adds translation layer (ALL languages)
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
            var layers = _inner.GetLayers(); // Delegácia na vnútorný display
            
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
        
        public string GetTooltipText()
        {
            var baseTooltip = _inner.GetTooltipText();
            return !string.IsNullOrEmpty(_translation) 
                ? $"{baseTooltip} → {_translation}" 
                : baseTooltip;
        }
        
        public string GetCssClass() => _inner.GetCssClass() + " has-translation";
    }
}