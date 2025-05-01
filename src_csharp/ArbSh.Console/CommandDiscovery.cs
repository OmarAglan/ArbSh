using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArbSh.Console.Commands; // Assuming commands are in this namespace

namespace ArbSh.Console
{
    /// <summary>
    /// Responsible for discovering available cmdlets.
    /// (Basic placeholder implementation using reflection)
    /// </summary>
    public static class CommandDiscovery
    {
        // Cache discovered cmdlets for performance
        private static Dictionary<string, Type>? _commandCache;

        /// <summary>
        /// Finds the Type of the cmdlet corresponding to the given command name.
        /// </summary>
        /// <param name="commandName">The name of the command (e.g., "Get-Help").</param>
        /// <returns>The Type of the cmdlet class, or null if not found.</returns>
        public static Type? Find(string commandName)
        {
            if (_commandCache == null)
            {
                BuildCache();
            }

            // PowerShell command names are case-insensitive
            _commandCache!.TryGetValue(commandName, out Type? cmdletType);
            return cmdletType;
        }

        /// <summary>
        /// Builds the cache of available commands by scanning the assembly.
        /// </summary>
        private static void BuildCache()
        {
            System.Console.WriteLine("DEBUG (Discovery): Building command cache...");
            _commandCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            // Get the assembly containing the cmdlets (assuming they are in the same assembly as CommandDiscovery)
            // TODO: Make this more robust, potentially scanning multiple assemblies or directories
            Assembly currentAssembly = Assembly.GetExecutingAssembly();

            var cmdletTypes = currentAssembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(CmdletBase)) && !t.IsAbstract);

            foreach (var type in cmdletTypes)
            {
                // Derive command name from class name (e.g., GetHelpCmdlet -> Get-Help)
                // TODO: Use a more robust naming convention or an attribute
                string className = type.Name;
                if (className.EndsWith("Cmdlet", StringComparison.OrdinalIgnoreCase))
                {
                    string potentialCommandName = className.Substring(0, className.Length - "Cmdlet".Length);
                    // Basic Verb-Noun format assumption
                    // This is very simplistic and needs improvement (e.g., using attributes)
                    string commandName = ConvertToVerbNoun(potentialCommandName);

                    if (!_commandCache.ContainsKey(commandName))
                    {
                        _commandCache.Add(commandName, type);
                        System.Console.WriteLine($"DEBUG (Discovery): Found cmdlet '{commandName}' -> {type.FullName}");
                    }
                    else
                    {
                         System.Console.WriteLine($"WARN (Discovery): Duplicate command name '{commandName}' detected for type {type.FullName}.");
                    }
                }
            }
             System.Console.WriteLine($"DEBUG (Discovery): Cache built with {_commandCache.Count} commands.");
        }

        /// <summary>
        /// Simple helper to convert PascalCase class name part to Verb-Noun.
        /// Example: GetHelp -> Get-Help
        /// (Very basic implementation)
        /// </summary>
        private static string ConvertToVerbNoun(string name)
        {
            // Find the position of the first uppercase letter after the first character
            int splitIndex = -1;
            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    splitIndex = i;
                    break;
                }
            }

            if (splitIndex > 0)
            {
                return $"{name.Substring(0, splitIndex)}-{name.Substring(splitIndex)}";
            }
            else
            {
                // If no second uppercase letter found, return as is (or handle differently)
                return name;
            }
        }
    }
}
