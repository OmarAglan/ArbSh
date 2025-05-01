using System;
using System.Collections.Generic;
using System.Linq;
using ArbSh.Console.Models; // Added to use CommandInfo

namespace ArbSh.Console.Commands
{
    /// <summary>
    /// Basic implementation of a cmdlet similar to PowerShell's Get-Command.
    /// Retrieves information about available commands.
    /// </summary>
    public class GetCommandCmdlet : CmdletBase
    {
        // TODO: Add parameters like -Name, -Noun, -Verb to filter commands

        /// <summary>
        /// Called once after pipeline input processing is complete.
        /// Retrieves and outputs command information.
        /// </summary>
        public override void EndProcessing()
        {
            // Get all discovered commands (mapping name to type)
            var allCommands = CommandDiscovery.GetAllCommands();

            if (allCommands == null || !allCommands.Any())
            {
                // TODO: Write Warning?
                System.Console.WriteLine("No commands found.");
                return;
            }

            // Create CommandInfo objects and write them to the pipeline
            // Order by name for consistent output
            foreach (var kvp in allCommands.OrderBy(c => c.Key))
            {
                var commandInfo = new CommandInfo(kvp.Key, kvp.Value);
                WriteObject(commandInfo);
            }
        }

        // No BeginProcessing or ProcessRecord needed as it generates output in EndProcessing
    }
}
