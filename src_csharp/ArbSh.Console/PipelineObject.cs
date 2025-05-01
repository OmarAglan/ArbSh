namespace ArbSh.Console
{
    /// <summary>
    /// Represents a base class or marker interface for objects flowing through the pipeline.
    /// Specific cmdlets might operate on or produce derived types.
    /// </summary>
    public class PipelineObject
    {
        // Basic properties or methods common to all pipeline objects can go here.
        // For now, it can be a simple marker or contain a raw value.
        public object? Value { get; set; }

        public PipelineObject(object? value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
    }
}
