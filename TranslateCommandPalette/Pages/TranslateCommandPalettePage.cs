// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using TranslateCommandPalette.Helpers;

namespace TranslateCommandPalette;

internal sealed partial class TranslateCommandPalettePage : DynamicListPage
{
    private readonly List<IListItem> _results = [];
    private readonly ListItem _EmptyItem;
    private readonly Translate translator = new Translate();

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
        IsLoading = true;
        if (oldSearch == newSearch)
        {
            return;
        }
        _results.Clear();
        if (string.IsNullOrEmpty(newSearch))
        {
            _results.Add(_EmptyItem);
        }
        else
        {
            var mandarin = translator.GetMandarinTranslation(newSearch).Result;
            _results.Add(new ListItem(new OpenUrl($"https://dict.youdao.com/result?word={newSearch}&lang=en")) { Title = mandarin });
        }
        RaiseItemsChanged();
        IsLoading = false;
    }
    public override IListItem[] GetItems() => _results.ToArray();
}
