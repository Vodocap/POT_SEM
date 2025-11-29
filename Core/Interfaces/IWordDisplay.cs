namespace POT_SEM.Core.Interfaces
{
    /// <summary>
    /// DECORATOR PATTERN - Component Interface
    /// Represents word with multiple display layers
    /// </summary>
    public interface IWordDisplay
    {
        List<DisplayLayer> GetLayers();
        string GetDisplayText();
        string GetTooltipText();
        string GetCssClass();
    }
    
    /// <summary>
    /// One display layer (original, furigana, romaji, translation)
    /// </summary>
    public class DisplayLayer
    {
        public required string Type { get; init; }
        public required string Text { get; init; }
        public required string CssClass { get; init; }
        public int Order { get; init; }
        
        public override string ToString() => $"[{Type}] {Text}";
    }
}