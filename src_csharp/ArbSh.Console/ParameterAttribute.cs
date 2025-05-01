using System;

namespace ArbSh.Console
{
    /// <summary>
    /// Attribute used to mark properties on CmdletBase derived classes
    /// as parameters that can be bound from the command line input.
    /// Inspired by System.Management.Automation.ParameterAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ParameterAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the position for positional binding.
        /// A value less than 0 indicates the parameter is not positional.
        /// </summary>
        public int Position { get; set; } = -1;

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is mandatory.
        /// </summary>
        public bool Mandatory { get; set; } = false;

        /// <summary>
        /// Gets or sets the parameter set name. Allows creating mutually exclusive parameters.
        /// (Placeholder for future use)
        /// </summary>
        public string? ParameterSetName { get; set; }

        /// <summary>
        /// Gets or sets help text for the parameter.
        /// (Placeholder for future use, e.g., by Get-Help)
        /// </summary>
        public string? HelpMessage { get; set; }

        // TODO: Add properties like ValueFromPipeline, ValueFromPipelineByPropertyName, etc.
    }
}
