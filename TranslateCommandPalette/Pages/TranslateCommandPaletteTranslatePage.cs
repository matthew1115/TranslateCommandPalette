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
using TranslateCommandPalette.Helpers;

namespace TranslateCommandPalette;

internal sealed partial class TranslateCommandPaletteTranslatePage : DynamicListPage, IDisposable
{
    private readonly List<IListItem> _results = [];
    private readonly ListItem _EmptyItem;
    private readonly Translate translate;
    private CancellationTokenSource _cts = new();

    public TranslateCommandPaletteTranslatePage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Translate";
        PlaceholderText = "Enter text to translate to Chinese";
        Name = "Open";

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
        }
        catch (FileNotFoundException ex)
        {
            Debug.WriteLine($"Model files missing: {ex.Message}");
            _results.Add(new ListItem(new OpenUrl("")) { Title = "Model dir not found. " + ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"Model loading failed: {ex.Message}");
            _results.Add(new ListItem(new OpenUrl("")) { Title = "Model dir not found. " + ex.Message });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error loading models: {ex.Message}");
            _results.Add(new ListItem(new OpenUrl("")) { Title = "Translation initialization error" + ex.GetType().Name + ": " + ex.Message });
        }
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        bool updateFlag = false;

        if (oldSearch == newSearch)
        {
            return;
        }
        IsLoading = true;
        _cts.Cancel();
        _cts = new();
        _results.Clear();
        if (string.IsNullOrEmpty(newSearch))
        {
            _results.Add(_EmptyItem);
        }
        else if (translate is null)
        {
            _results.Add(new ListItem(new OpenUrl("")) { Title = "Translation unavailable: Models are not loaded" });
        }
        else
        {
            try
            {
                var TranslatedText = translate.GetTargetTranslation(newSearch, _cts.Token).Result;
                if (TranslatedText != "@Canceled")
                {
                    updateFlag = true;
                    _results.Add(new ListItem(new OpenUrl($"https://dict.youdao.com/result?word={newSearch}&lang=en")) { Title = TranslatedText });
                }
            }
            catch (OperationCanceledException)
            {
                // ignored; user changed the query quickly
            }
            catch (AggregateException aex) when (aex.InnerException is OperationCanceledException)
            {
                // Task.Result wraps OperationCanceledException in AggregateException
            }
            catch (Exception ex)
            {
                _results.Add(new ListItem(new OpenUrl("")) { Title = "Translation models not found. Error:" + ex.Message });
            }
        }
        if (updateFlag)
            RaiseItemsChanged(0);
        IsLoading = false;
    }
    public override IListItem[] GetItems() => _results.ToArray();

    public void Dispose()
    {
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }
}
