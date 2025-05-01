using System.Collections.Generic;

namespace ArbSh.Console
{
    /// <summary>
    /// Represents a single command parsed from the input line.
    /// Includes the command name and any arguments/parameters.
    /// (This structure will likely evolve as parsing becomes more sophisticated)
    /// </summary>
    public class ParsedCommand
    {
        /// <summary>
        /// The name of the command (e.g., "Write-Output", "Get-Content", "احصل-محتوى").
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// A list of arguments or parameters provided for the command.
        /// TODO: Differentiate between positional arguments and named parameters.
        /// For now, just a list of strings (positional arguments).
        /// </summary>
        public List<string> Arguments { get; }

        /// <summary>
        /// A dictionary to hold named parameters and their values.
        /// Keys are parameter names (e.g., "-CommandName", "-Path").
        /// Values are the string representation of the parameter value.
        /// TODO: Handle switch parameters (no value).
        /// TODO: Handle parameter value type conversion later.
        /// </summary>
        public Dictionary<string, string> Parameters { get; }

        // TODO: Add properties for redirections (Input, Output, Error)
        // TODO: Add information about pipeline position (start, middle, end)

        public ParsedCommand(string commandName, List<string> arguments, Dictionary<string, string> parameters)
        {
            CommandName = commandName;
            Arguments = arguments ?? new List<string>();
            Parameters = parameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // Case-insensitive keys
        }
    }
}
