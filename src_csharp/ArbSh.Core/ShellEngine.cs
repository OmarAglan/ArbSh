namespace ArbSh.Core;

/// <summary>
/// High-level shell entry point for hosts.
/// </summary>
public static class ShellEngine
{
    /// <summary>
    /// Parses and executes a single input line.
    /// </summary>
    /// <param name="inputLine">The logical input line.</param>
    /// <param name="sink">The host sink for output.</param>
    /// <param name="options">Execution options.</param>
    public static void ExecuteInput(string inputLine, IExecutionSink sink, ExecutionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(inputLine);
        ArgumentNullException.ThrowIfNull(sink);

        using var scope = CoreConsole.PushSink(sink, options);
        var commands = Parser.Parse(inputLine);
        Executor.Execute(commands, sink, options);
    }
}
