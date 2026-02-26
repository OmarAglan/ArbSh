using System;

namespace ArbSh.Core.Models
{
    /// <summary>
    /// Represents information about a discovered command (cmdlet).
    /// </summary>
    public class CommandInfo
    {
        /// <summary>
        /// Gets the name of the command (e.g., "مساعدة").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the .NET type that implements the command.
        /// </summary>
        public Type ImplementingType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandInfo"/> class.
        /// </summary>
        /// <param name="name">The command name.</param>
        /// <param name="implementingType">The implementing type.</param>
        public CommandInfo(string name, Type implementingType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ImplementingType = implementingType ?? throw new ArgumentNullException(nameof(implementingType));
        }

        /// <summary>
        /// Returns a string representation of the command info (primarily the name).
        /// </summary>
        public override string ToString()
        {
            return Name; // Default string representation is just the command name
        }
    }
}

