using POT_SEM.Core.Interfaces;

namespace POT_SEM.Services.Patterns.Decorator.WordDisplay
{
    /// <summary>
    /// DECORATOR - Adds furigana (hiragana) layer for Japanese words
    /// </summary>
    public class FuriganaDecorator : IWordDisplay
    {
        private readonly IWordDisplay _inner;
        private readonly string? _furigana;

        public FuriganaDecorator(IWordDisplay inner, string? furigana)
        {
            _inner = inner;
            _furigana = furigana;
        }

        public List<DisplayLayer> GetLayers()
        {
            var layers = _inner.GetLayers();

            if (!string.IsNullOrEmpty(_furigana))
            {
                layers.Add(new DisplayLayer
                {
                    Type = "furigana",
                    Text = _furigana,
                    CssClass = "word-furigana",
                    Order = 5
                });
            }

            return layers;
        }

        public string GetDisplayText() => _inner.GetDisplayText();

        public string GetTooltipText()
        {
            var baseTooltip = _inner.GetTooltipText();
            return !string.IsNullOrEmpty(_furigana)
                ? $"{baseTooltip} ({_furigana})"
                : baseTooltip;
        }

        public string GetCssClass() => _inner.GetCssClass() + " has-furigana";
    }
}
