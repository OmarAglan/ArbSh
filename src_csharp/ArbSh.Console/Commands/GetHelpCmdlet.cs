using System;
using System.Collections.Generic;
using System.Linq; // Needed for Where/OrderBy
using System.Reflection; // For ParameterInfo
using System.Text; // For StringBuilder

namespace ArbSh.Console.Commands
{
    /// <summary>
    /// Placeholder implementation for a Get-Help cmdlet.
    /// </summary>
    [ArabicName("احصل-مساعدة")] // Added Arabic name attribute
    public class GetHelpCmdlet : CmdletBase
    {
        [Parameter(Position = 0, HelpMessage = "The name of the command to get help for.")]
        public string? CommandName { get; set; }

        [Parameter(HelpMessage = "Display the full help topic.")]
        public bool Full { get; set; } // Switch parameter, defaults to false

        // TODO: Add other parameters like -Category etc.

        public override void EndProcessing()
        {
            if (!string.IsNullOrEmpty(CommandName))
            {
                // Find the command type
                Type? targetCmdletType = CommandDiscovery.Find(CommandName);

                if (targetCmdletType != null)
                {
                    DisplayCommandHelp(targetCmdletType);
                }
                else
                {
                    WriteObject($"Help Error: Command '{CommandName}' not found.");
                    // TODO: Use WriteError
                }
            }
            else // General help
            {
                 // TODO: Implement more comprehensive general help / list modules etc.
                 WriteObject("Placeholder general help message. Try 'Get-Help <Command-Name>'.");
                 WriteObject("Example: Get-Help Get-Command");
                 // Optionally list all commands if -Full is specified?
                 if (Full)
                 {
                     WriteObject("\nAvailable Commands (use Get-Command for details):");
                     var allCommands = CommandDiscovery.GetAllCommands();
                     foreach(var cmd in allCommands.OrderBy(kvp => kvp.Key))
                     {
                         WriteObject($"  {cmd.Key}");
                     } // <-- Added missing closing brace
                 }
            }
        }

        private void DisplayCommandHelp(Type cmdletType)
        {
            var helpBuilder = new StringBuilder();
            string commandName = CommandDiscovery.GetAllCommands()
                                    .FirstOrDefault(kvp => kvp.Value == cmdletType).Key ?? cmdletType.Name;

            helpBuilder.AppendLine($"\nNAME");
            helpBuilder.AppendLine($"    {commandName}");

            // TODO: Get synopsis/description from an attribute on the class?
            helpBuilder.AppendLine($"\nSYNOPSIS");
            helpBuilder.AppendLine($"    (Synopsis for {commandName} not available)");


            helpBuilder.AppendLine($"\nSYNTAX");
            // Build basic syntax line
            helpBuilder.Append($"    {commandName}");
            var parameters = cmdletType.GetProperties()
                .Select(p => new { Property = p, Attr = p.GetCustomAttribute<ParameterAttribute>() })
                .Where(p => p.Attr != null)
                .OrderBy(p => p.Attr!.Position >= 0 ? p.Attr.Position : int.MaxValue) // Positional first
                .ThenBy(p => p.Property.Name)
                .ToList();

            foreach (var paramInfo in parameters)
            {
                string paramSyntax = $"[-{paramInfo.Property.Name}";
                // Indicate type (basic) - PowerShell shows type like [<Type>]
                // Handle boolean switch parameters specially in syntax
                if (paramInfo.Property.PropertyType != typeof(bool))
                {
                    paramSyntax += $" <{paramInfo.Property.PropertyType.Name}>";
                }
                paramSyntax += "]";
                // Positional indication removed for simplicity for now, focus on named
                // if (paramInfo.Attr!.Position >= 0)
                // {
                //     paramSyntax += $" [[-{paramInfo.Property.Name}]]"; // Indicate positional alternative
                // }
                 if (!paramInfo.Attr!.Mandatory) // Wrap optional in []
                 {
                     // Note: This syntax representation is very basic
                     // PowerShell syntax is more complex (parameter sets etc.)
                     // The outer [] already indicates optionality for named params
                 }
                helpBuilder.Append($" {paramSyntax}");
            }
            helpBuilder.AppendLine();


            // TODO: Add DESCRIPTION section

            if (Full && parameters.Any())
            {
                helpBuilder.AppendLine($"\nPARAMETERS");
                foreach (var paramInfo in parameters)
                {
                    helpBuilder.AppendLine($"    -{paramInfo.Property.Name} <{paramInfo.Property.PropertyType.Name}>");
                    if (!string.IsNullOrEmpty(paramInfo.Attr!.HelpMessage))
                    {
                        helpBuilder.AppendLine($"        {paramInfo.Attr.HelpMessage}");
                    }
                    helpBuilder.AppendLine($"        Required?                    {(paramInfo.Attr.Mandatory ? "True" : "False")}");
                    helpBuilder.AppendLine($"        Position?                    {(paramInfo.Attr.Position >= 0 ? paramInfo.Attr.Position.ToString() : "Named")}");
                    helpBuilder.AppendLine($"        Accepts pipeline input?      {(paramInfo.Attr.ValueFromPipeline ? "True (By Value)" : (paramInfo.Attr.ValueFromPipelineByPropertyName ? "True (By Property Name)" : "False"))}");
                    // TODO: Add Default value? Aliases?
                    helpBuilder.AppendLine();
                }
            }

            // TODO: Add EXAMPLES section
            // TODO: Add RELATED LINKS section

            WriteObject(helpBuilder.ToString());
        }
    }
}
