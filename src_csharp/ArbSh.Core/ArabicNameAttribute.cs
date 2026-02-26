using System;

namespace ArbSh.Core
{
    /// <summary>
    /// Specifies the Arabic name (alias) for a cmdlet.
    /// This allows invoking the cmdlet using its Arabic name in the shell.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)] // Allow on properties too
    public sealed class ArabicNameAttribute : Attribute
    {
        /// <summary>
        /// Gets the Arabic name assigned to the cmdlet.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArabicNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The Arabic name for the cmdlet.</param>
        public ArabicNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Arabic name cannot be null or whitespace.", nameof(name));
            }
            Name = name;
        }
    }
}

