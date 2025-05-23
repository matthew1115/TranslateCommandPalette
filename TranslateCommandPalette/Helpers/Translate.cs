using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides functionality for translating English words to Mandarin Chinese.
/// Uses Wiktionary's API to retrieve translations.
/// </summary>
namespace TranslateCommandPalette.Helpers
{
    public class Translate
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="Translate"/> class.
        /// Creates an HttpClient instance that will be used for API requests.
        /// </summary>
        public Translate()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Retrieves the Mandarin translation for an English word from Wiktionary.
        /// </summary>
        /// <param name="word">The English word to translate.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains:
        /// - The Mandarin translation if found.
        /// - Error message if no translation is found.
        /// - Error message if an exception occurs during the request or parsing.
        /// </returns>
        /// <example>
        /// Usage example:
        /// <code>
        /// var translator = new GetTranslate();
        /// string translation = await translator.GetMandarinTranslation("hello");
        /// Console.WriteLine(translation); // Outputs the Mandarin translation for "hello"
        /// </code>
        /// </example>
        public async Task<string> GetMandarinTranslation(string word, CancellationToken cancellationToken = default)
        {
            string url = $"https://en.wiktionary.org/w/api.php?action=query&prop=revisions&titles={word}&rvprop=content&format=json";

            try
            {
                // Pass the cancellation token to GetAsync
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Pass the cancellation token to ReadAsStringAsync
                string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using JsonDocument document = JsonDocument.Parse(jsonContent);

                // Navigate through the JSON structure
                var pages = document.RootElement.GetProperty("query").GetProperty("pages");
                var pageId = pages.EnumerateObject().First().Name;
                var wikitext = pages.GetProperty(pageId)
                                    .GetProperty("revisions")[0]
                                    .GetProperty("*")
                                    .GetString();

                if (wikitext == null)
                    return $"No content found for '{word}'.";

                // Find the Mandarin line using regex
                var mandarinMatch = Regex.Match(
                    wikitext,
                    @"\*: Mandarin: \{\{.*?\|.*?\|([^}|]+)"
                );

                if (!mandarinMatch.Success)
                    return $"No Mandarin translation found for '{word}'.";

                // Extract the Mandarin word
                string mandarinWord = mandarinMatch.Groups[1].Value;
                return $"{mandarinWord}";
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation specifically
                return "Translation cancelled";
            }
            catch (HttpRequestException e)
            {
                return $"Request failed: {e.Message}";
            }
            catch (JsonException e)
            {
                return $"Parsing error: {e.Message}";
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }
        }
    }
}