namespace ArbSh.Console
{
    /// <summary>
    /// Represents a single object flowing through the pipeline.
    /// Can represent regular output or an error record.
    /// </summary>
    public class PipelineObject
    {
        // Basic properties or methods common to all pipeline objects can go here.
        public object? Value { get; } // Make Value readonly after construction
        public bool IsError { get; } // Flag to indicate if this is an error

        // Constructor for regular output
        public PipelineObject(object? value) : this(value, false) { }

        // Primary constructor
        public PipelineObject(object? value, bool isError)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
    }
}
