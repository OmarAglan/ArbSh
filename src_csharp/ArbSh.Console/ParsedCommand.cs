using System.Collections.Generic;

namespace ArbSh.Console
{
    /// <summary>
    /// Represents a single command parsed from the input line.
    /// Includes the command name, arguments/parameters, and redirection rules.
    /// </summary>
    public class ParsedCommand
    {
        /// <summary>
        /// Defines the type of target for redirection.
        /// </summary>
        public enum RedirectionTargetType
        {
            /// <summary>Redirect to a file path.</summary>
            FilePath,
            /// <summary>Redirect to another stream handle (e.g., &1 for stdout, &2 for stderr).</summary>
            StreamHandle
        }

        /// <summary>
        /// Holds information about a single redirection rule.
        /// </summary>
        public struct RedirectionInfo
        {
            /// <summary>The source stream handle (1 for stdout, 2 for stderr, etc.).</summary>
            public int SourceStreamHandle { get; }
            /// <summary>The type of the redirection target.</summary>
            public RedirectionTargetType TargetType { get; }
            /// <summary>The target file path or stream handle string (e.g., "output.txt", "&1").</summary>
            public string Target { get; }
            /// <summary>Indicates if redirection to a file should append.</summary>
            public bool Append { get; } // Only relevant for FilePath target

            public RedirectionInfo(int sourceStreamHandle, RedirectionTargetType targetType, string target, bool append)
            {
                SourceStreamHandle = sourceStreamHandle;
                TargetType = targetType;
                Target = target;
                Append = append && targetType == RedirectionTargetType.FilePath; // Append only makes sense for files
            }

            public override string ToString()
            {
                string op = (TargetType == RedirectionTargetType.FilePath && Append) ? ">>" : ">";
                string source = SourceStreamHandle == 1 ? "" : SourceStreamHandle.ToString(); // Don't show "1" for stdout
                return $"{source}{op}{Target}";
            }
        }

        /// <summary>
        /// The name of the command (e.g., "Write-Output", "Get-Content", "احصل-محتوى").
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// A list of positional arguments provided for the command.
        /// Elements can be strings (for literal arguments) or List<ParsedCommand> (for sub-expressions).
        /// </summary>
        public List<object> Arguments { get; } // Changed from List<string> to List<object>

        /// <summary>
        /// A dictionary to hold named parameters and their values.
        /// Keys are parameter names (e.g., "-Path", "-اسم"). Values are the string representation.
        /// </summary>
        public Dictionary<string, string> Parameters { get; }

        /// <summary>
        /// Gets the list of redirection rules applied to this command.
        /// </summary>
        public List<RedirectionInfo> Redirections { get; }

        // TODO: Add properties for input redirection (<)
        // TODO: Add information about pipeline position (start, middle, end)

        public ParsedCommand(string commandName, List<object> arguments, Dictionary<string, string> parameters) // Updated argument type
        {
            CommandName = commandName;
            Arguments = arguments ?? new List<object>(); // Changed from List<string>
            Parameters = parameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // Case-insensitive keys
            Redirections = new List<RedirectionInfo>(); // Initialize redirection list
        }

        /// <summary>
        /// Internal method used by the parser to add a parsed redirection rule.
        /// </summary>
        internal void AddRedirection(RedirectionInfo redirection)
        {
            Redirections.Add(redirection);
            System.Console.WriteLine($"DEBUG (ParsedCommand): Added redirection: {redirection}");
        }
    }
}
