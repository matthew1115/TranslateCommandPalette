using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslateCommandPalette.Helpers
{
    internal sealed partial class OpenUrl : InvokableCommand
    {
        private readonly string _url;
        public OpenUrl(string Url)
        {
            _url = Url;
        }
        public override ICommandResult Invoke()
        {
            Console.WriteLine($"Opening URL: {_url}");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                Arguments = _url,
            };
            Process.Start(startInfo);
            return CommandResult.Dismiss();
        }
    }
}
