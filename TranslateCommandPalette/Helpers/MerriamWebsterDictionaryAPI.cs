using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TranslateCommandPalette.Helpers
{
    /// <summary>
    /// Represents a single dictionary entry from the API response.
    /// We only map the 'shortdef' property since that's all we need for this task.
    /// The JsonPropertyName attribute links this property to the "shortdef" field in the JSON.
    /// </summary>
    public class DictionaryEntry
    {
        [JsonPropertyName("shortdef")]
        public List<string> ShortDefinitions { get; set; } = new List<string>();
    }

    /// <summary>
    /// A class to parse data from the Merriam-Webster Collegiate Dictionary API.
    /// It handles making the HTTP request and deserializing the JSON response to extract definitions.
    /// </summary>
    public class DictionaryApiParser
    {
        private readonly string _apiKey;
        // HttpClient is intended to be instantiated once and reused throughout the life of an application.
        private static readonly HttpClient HttpClient = new HttpClient();
        private const string ApiUrlFormat = "https://www.dictionaryapi.com/api/v3/references/collegiate/json/{0}?key={1}";

        /// <summary>
        /// Initializes a new instance of the DictionaryApiParser.
        /// </summary>
        /// <param name="apiKey">Your Merriam-Webster Collegiate Dictionary API key.</param>
        public DictionaryApiParser(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
            }
            _apiKey = apiKey;
        }

        /// <summary>
        /// Asynchronously fetches a list of short definitions for a given word.
        /// </summary>
        /// <param name="word">The word to look up.</param>
        /// <returns>A Task that resolves to a list of short definitions. Returns an empty list if the word is not found or an error occurs.</returns>
        public async Task<List<string>> GetShortDefinitionsAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                // Return an empty list for invalid input.
                return new List<string>();
            }

            // Format the request URL with the word and API key.
            string requestUrl = string.Format(ApiUrlFormat, Uri.EscapeDataString(word), _apiKey);

            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode(); // Throws an exception if the HTTP response status is an error code.

                string jsonResponse = await response.Content.ReadAsStringAsync();

                // The API returns an empty JSON array for some invalid queries.
                if (string.IsNullOrWhiteSpace(jsonResponse) || jsonResponse == "[]")
                {
                    return new List<string>();
                }

                // The API response can be complex. It might return an array of definition objects
                // or an array of strings (suggestions) if the word isn't found. We check the first
                // character to see if we're dealing with an array of objects.
                if (jsonResponse.TrimStart().StartsWith("["))
                {
                    var options = new JsonSerializerOptions
                    {
                        // Allows for case-insensitive matching between JSON properties and C# properties.
                        PropertyNameCaseInsensitive = true
                    };

                    // Try to deserialize the JSON array into a list of our DictionaryEntry objects.
                    var dictionaryEntries = JsonSerializer.Deserialize<List<DictionaryEntry>>(jsonResponse, options);

                    // If deserialization is successful and we have entries, return the short definitions from the first entry.
                    if (dictionaryEntries != null && dictionaryEntries.Count > 0 && dictionaryEntries[0].ShortDefinitions != null)
                    {
                        return dictionaryEntries[0].ShortDefinitions;
                    }
                }
            }
            catch (HttpRequestException e)
            {
                // Handle network-related errors.
                Console.WriteLine($"Error fetching data from the API: {e.Message}");
            }
            catch (JsonException e)
            {
                // This can happen if the API returns suggestions (an array of strings) instead of definitions (an array of objects).
                // For this specific request, we treat suggestions as "not found".
                Console.WriteLine($"Could not parse definition for '{word}'. It may not be in the dictionary or the API returned suggestions. Details: {e.Message}");
            }
            catch (Exception e)
            {
                // Handle any other unexpected errors.
                Console.WriteLine($"An unexpected error occurred: {e.Message}");
            }

            // Return an empty list if any part of the process fails.
            return new List<string>();
        }
    }
}