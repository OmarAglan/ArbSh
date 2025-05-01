using System;
using System.Collections.Generic;
using System.Linq; // Added for LINQ methods

namespace ArbSh.Console.Commands
{
    /// <summary>
    /// Placeholder implementation for a Get-Command cmdlet.
    /// Lists available commands (currently hardcoded).
    /// </summary>
    public class GetCommandCmdlet : CmdletBase
    {
        // TODO: Add parameters like -Name, -Verb, -Noun, etc.
        [Parameter(Position = 0)]
        public string? Name { get; set; } // Example parameter for filtering by name

        public override void EndProcessing()
        {
            // Get discovered commands from the CommandDiscovery cache
            // Need access to the cache - let's add a helper method to CommandDiscovery
            var discoveredCommands = CommandDiscovery.GetAllCommands(); // Assuming this method exists

            IEnumerable<KeyValuePair<string, Type>> commandsToDisplay = discoveredCommands;

            if (!string.IsNullOrEmpty(Name))
            {
                // Filter based on the Name parameter (can use wildcards later)
                // Basic filtering for now: exact match or starts with
                 commandsToDisplay = discoveredCommands
                    .Where(kvp => kvp.Key.Equals(Name, StringComparison.OrdinalIgnoreCase) || kvp.Key.StartsWith(Name, StringComparison.OrdinalIgnoreCase));
            }

            if (commandsToDisplay.Any())
            {
                 if (string.IsNullOrEmpty(Name)) // Only print header if listing all
                 {
                     WriteObject("Available Commands:");
                 }
                 foreach (var kvp in commandsToDisplay)
                 {
                     // TODO: Output richer command info objects (Name, Type, Module, etc.)
                     WriteObject($"  {kvp.Key} (Type: {kvp.Value.Name})");
                 } // <-- Added missing closing brace
            }
            else
            {
                 // Use WriteError or similar mechanism in the future
                 WriteObject($"Command not found matching: {Name ?? "*"}");
            }
        }
    }
}
