// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace TranslateCommandPalette;

public partial class TranslateCommandPaletteCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public TranslateCommandPaletteCommandsProvider()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new TranslateCommandPaletteTranslatePage()) { Title = "Translate" },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
