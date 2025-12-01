namespace POT_SEM.Services.Patterns.Bridge.Parsing
{
    /// <summary>
    /// FACTORY - Creates parser with appropriate Bridge configuration
    /// </summary>
    public class LanguageParserFactory
    {
        public static LanguageParser CreateParser(string languageCode)
        {
            var splitter = new RegexTextSplitter();
            
            return languageCode.ToLower() switch
            {
                "ja" => new JapaneseLanguageParser(
                    splitter,
                    new ScriptBasedTokenizer()),
                    
                "ar" => new ArabicLanguageParser(
                    splitter,
                    new SpaceBasedTokenizer()),
                    
                _ => new LatinLanguageParser(
                    splitter,
                    new SpaceBasedTokenizer())
            };
        }
    }
}
