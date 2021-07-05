﻿using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace TestExtension
{
    [Command(PackageIds.MultiInstanceWindow)]
    internal sealed class MultiInstanceWindowCommand : BaseCommand<MultiInstanceWindowCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            // Create the window with the first free ID.
            for (int i = 0; i < 10; i++)
            {
                ToolWindowPane window = await MultiInstanceWindow.ShowAsync(id: i, create: false);

                if (window == null)
                {
                    await MultiInstanceWindow.ShowAsync(id: i, create: true);
                    break;
                }
            }
        }
    }
}
