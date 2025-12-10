namespace POT_SEM.Services.Patterns.ChainOfResponsibility.Translation
{
    /// <summary>
    /// Base handler class for the translation chain.
    /// </summary>
    public abstract class TranslationHandler
    {
        protected TranslationHandler? _nextHandler;
        
        public TranslationHandler SetNext(TranslationHandler handler)
        {
            _nextHandler = handler;
            return handler;
        }
        
        /// <summary>
        /// Handle the translation request
        /// returns the translated string or null if the chain ends without a result
        /// </summary>
        public abstract Task<string?> HandleAsync(string word, string sourceLang, string targetLang);
    }
}
