using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CTranslate2Wrapper;

/// <summary>
/// Provides functionality for translating English words to Mandarin Chinese.
/// Uses Wiktionary's API to retrieve translations.
/// </summary>
namespace TranslateCommandPalette.Helpers
{
    public class Translate
    {
        private Translator mulEnTranslator;
        private Translator EnTargetTranslator;
        /// <summary>
        /// Initializes a new instance of the <see cref="Translate"/> class.
        /// Creates an HttpClient instance that will be used for API requests.
        /// </summary>
        public Translate(string mulEnPath, string EnZhPath)
        {
            mulEnTranslator = new(mulEnPath);
            EnTargetTranslator = new(EnZhPath);
        }

        // TODO: Add language detection and support multilang
        public async Task<string> GetTargetTranslation(string text, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var options = new TranslationOptions
                {
                    // The callback is called for each token generation step.
                    callback = step =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return true; // Stop translation early
                        return false; // Continue translation
                    }
                };

                var result = EnTargetTranslator.Translate(text, options);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }, cancellationToken);
        }
    }
}