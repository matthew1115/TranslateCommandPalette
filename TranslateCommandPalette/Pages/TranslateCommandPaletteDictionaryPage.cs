using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TranslateCommandPalette.Helpers;

internal sealed class TranslateCommandPaletteDictionaryPage : DynamicListPage
{
    private readonly List<IListItem> _results = [];
    private readonly ListItem _EmptyItem;
    private readonly Translate translator = new();
    private CancellationTokenSource _cts = new();

    public TranslateCommandPaletteDictionaryPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Translate";
        PlaceholderText = "Enter text to lookup";
        Name = "Open";
    }
    public override void UpdateSearchText(string oldSearch, string newSearch)
    {

    }
}
