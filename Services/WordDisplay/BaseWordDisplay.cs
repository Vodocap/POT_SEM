using POT_SEM.Core.Interfaces;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.WordDisplay
{
    /// <summary>
    /// DECORATOR PATTERN - Concrete Component
    /// Basic word display (just original text)
    /// </summary>
    public class BaseWordDisplay : IWordDisplay
    {
        protected readonly ProcessedWord _word;
        
        public BaseWordDisplay(ProcessedWord word)
        {
            _word = word;
        }
        
        public List<DisplayLayer> GetLayers()
        {
            return new List<DisplayLayer>
            {
                new DisplayLayer
                {
                    Type = "original",
                    Text = _word.Original,
                    CssClass = "word-original",
                    Order = 0
                }
            };
        }
        
        public string GetDisplayText() => _word.Original;
        
        public string GetTooltipText() => _word.Original;
        
        public string GetCssClass()
        {
            return _word.IsPunctuation ? "word-punctuation" : "word-base";
        }
    }
}