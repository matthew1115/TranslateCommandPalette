using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

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
            // Minimal validation for common issues
            //if (!Directory.Exists(mulEnPath))
            //    throw new DirectoryNotFoundException($"Model directory not found: {mulEnPath}");
            //if (!Directory.Exists(EnZhPath))
            //    throw new DirectoryNotFoundException($"Model directory not found: {EnZhPath}");

            // Check for required files that CTranslate2 typically needs
            var requiredFiles = new[] { "model.bin", "config.json", "vocabulary.txt", "shared_vocabulary.txt" };
            foreach (var dir in new[] { (mulEnPath, "mulEn"), (EnZhPath, "enZh") })
            {
                var missingFiles = requiredFiles.Where(file => !File.Exists(Path.Combine(dir.Item1, file))).ToList();
                if (missingFiles.Any())
                {
                    Debug.WriteLine($"Warning: {dir.Item2} model missing files: {string.Join(", ", missingFiles)}");
                }
            }

            // Normalize paths for C++ (replace backslashes with forward slashes)
            var normMulEnPath = mulEnPath.Replace('\\', '/');
            var normEnZhPath = EnZhPath.Replace('\\', '/');

            Debug.WriteLine($"Attempting to load models from:");
            Debug.WriteLine($"  MulEn: {normMulEnPath}");
            Debug.WriteLine($"  EnZh: {normEnZhPath}");

            try
            {
                Debug.WriteLine("Loading MulEn translator...");
                mulEnTranslator = new(normMulEnPath);
                Debug.WriteLine("MulEn translator loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load MulEn translator: {ex.Message}");
                throw new InvalidOperationException($"Failed to load MulEn model from '{normMulEnPath}': {ex.Message}", ex);
            }

            try
            {
                Debug.WriteLine("Loading EnZh translator...");
                EnTargetTranslator = new(normEnZhPath);
                Debug.WriteLine("EnZh translator loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load EnZh translator: {ex.Message}");
                mulEnTranslator?.Dispose(); // Clean up the first translator if second fails
                throw new InvalidOperationException($"Failed to load EnZh model from '{normEnZhPath}': {ex.Message}", ex);
            }
        }

        // TODO: Add language detection and support multilang
        public async Task<string> GetTargetTranslation(string text, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var result = EnTargetTranslator.Translate(text);
                    cancellationToken.ThrowIfCancellationRequested();
                    return result;
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return "@Canceled";
            }
        }
    }
}