// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TranslateCommandPalette.Helpers;
using Windows.ApplicationModel.Appointments;

namespace TranslateCommandPalette;

internal sealed partial class TranslateCommandPaletteTranslatePage : DynamicListPage, IDisposable
{
    private readonly List<IListItem> _results = [];
    private readonly ListItem _EmptyItem;
    private readonly Translate translate;
    private CancellationTokenSource _cts = new();
    private readonly object _delayLock = new();
    private long _lastQueryTick;

    public TranslateCommandPaletteTranslatePage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Translate";
        PlaceholderText = "Enter text to translate to Chinese";
        Name = "Open";

        // Friendly empty state, consistent with the other page
        this.EmptyContent = new CommandItem
        {
            Title = PlaceholderText,
            Icon = this.Icon
        };

        // Default empty item
        //_EmptyItem = new ListItem(new OpenUrl("")) { Title = "Enter text to translate to Chinese" };

        // Initialize translators using absolute model paths from app base directory
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var mulEnPath = Path.Combine(baseDir, "Models", "opus_mul_en_ct2_int8");
            var enZhPath = Path.Combine(baseDir, "Models", "opus_en_zh_ct2_int8");

            Debug.WriteLine($"Base directory: {baseDir}");
            Debug.WriteLine($"Looking for models at:");
            Debug.WriteLine($"  {mulEnPath}");
            Debug.WriteLine($"  {enZhPath}");

            translate = new Translate(mulEnPath, enZhPath);
            Debug.WriteLine("Translation models loaded successfully");
        }
        catch (DirectoryNotFoundException ex)
        {
            Debug.WriteLine($"Directory not found: {ex.Message}");
            _results.Add(new ListItem(new OpenUrl("")) { Title = "Model dir not found. " + ex.Message });
            this.EmptyContent = new CommandItem { Title = "Model directory not found", Icon = this.Icon };
        }
        catch (FileNotFoundException ex)
        {
            Debug.WriteLine($"Model files missing: {ex.Message}");
            _results.Add(new ListItem(new OpenUrl("")) { Title = "Model files missing. " + ex.Message });
            this.EmptyContent = new CommandItem { Title = "Model files missing", Icon = this.Icon };
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"Model loading failed: {ex.Message}");
            _results.Add(new ListItem(new OpenUrl("")) { Title = "Model loading failed. " + ex.Message });
            this.EmptyContent = new CommandItem { Title = "Model loading failed", Icon = this.Icon };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error loading models: {ex.Message}");
            _results.Add(new ListItem(new OpenUrl("")) { Title = "Translation initialization error " + ex.GetType().Name + ": " + ex.Message });
            this.EmptyContent = new CommandItem { Title = "Translation initialization error", Icon = this.Icon };
        }
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        // Debounce + non-blocking translation similar to PowerTranslatorExtensionPage
        long thisTick;
        lock (_delayLock)
        {
            thisTick = ++_lastQueryTick;
        }

        // If user cleared or input is too short, reset quickly
        if (string.IsNullOrWhiteSpace(newSearch) || newSearch.Length < 3)
        {
            _cts.Cancel();
            _results.Clear();
            IsLoading = false;
            RaiseItemsChanged(0);
            return;
        }

        // Start debounced background work
        IsLoading = true;
        Task.Run(async () =>
        {
            try
            {
                // Debounce window
                await Task.Delay(500).ConfigureAwait(false);

                // If another keystroke happened, abort this run
                if (thisTick != _lastQueryTick)
                {
                    return;
                }

                if (translate is null)
                {
                    _results.Clear();
                    _results.Add(new ListItem(new OpenUrl("")) { Title = "Translation unavailable: Models are not loaded" });
                    RaiseItemsChanged(0);
                    return;
                }

                // Cancel any in-flight translation and create a fresh token
                _cts.Cancel();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                string translated = await translate.GetTargetTranslation(newSearch, token).ConfigureAwait(false);

                if (translated == "@Canceled" || token.IsCancellationRequested)
                {
                    return; // User typed again
                }

                // Prepare results
                var youdaoUrl = $"https://dict.youdao.com/result?word={Uri.EscapeDataString(newSearch)}&lang=en";

                _results.Clear();
                _results.Add(new ListItem(new OpenUrl(youdaoUrl)) { Title = translated });

                // Notify UI
                RaiseItemsChanged(0);
            }
            catch (OperationCanceledException)
            {
                // Swallow; expected when user keeps typing
            }
            catch (Exception ex)
            {
                _results.Clear();
                _results.Add(new ListItem(new OpenUrl("")) { Title = "Translation error: " + ex.Message });
                RaiseItemsChanged(0);
            }
            finally
            {
                // Only hide loading if this is still the latest request
                if (thisTick == _lastQueryTick)
                {
                    IsLoading = false;
                }
            }
        });
    }
    public override IListItem[] GetItems() => _results.ToArray();

    public void Dispose()
    {
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }
}
