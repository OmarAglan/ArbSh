using System;

namespace ArbSh.Console
{
    /// <summary>
    /// Represents errors that occur during parameter binding.
    /// </summary>
    public class ParameterBindingException : Exception
    {
        public ParameterBindingException() { }

        public ParameterBindingException(string message)
            : base(message) { }

        public ParameterBindingException(string message, Exception inner)
            : base(message, inner) { }

        // Add any additional properties if needed, e.g., ParameterName
        public string? ParameterName { get; set; }
    }
}
