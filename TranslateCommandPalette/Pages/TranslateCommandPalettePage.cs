// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading;
using TranslateCommandPalette.Helpers;

namespace TranslateCommandPalette;

internal sealed partial class TranslateCommandPalettePage : DynamicListPage, IDisposable
{
    private readonly List<IListItem> _results = [];
    private readonly ListItem _EmptyItem;
    private readonly Translate translator = new();
    private CancellationTokenSource _cts = new();

    public TranslateCommandPalettePage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Translate";
        Name = "Open";
        _EmptyItem = new ListItem(new NoOpCommand())
        {
            Title = "Input to translate to Chinese",
        };
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
        else
        {
            var mandarin = translator.GetMandarinTranslation(newSearch, _cts.Token).Result;
            if (mandarin != "@Canceled")
            {
                updateFlag = true;
                _results.Add(new ListItem(new OpenUrl($"https://dict.youdao.com/result?word={newSearch}&lang=en")) { Title = mandarin });
            }
        }
        if (updateFlag)
        {
            RaiseItemsChanged(0);
            IsLoading = false;
        }
    }
    public override IListItem[] GetItems() => _results.ToArray();

    public void Dispose()
    {
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }
}
