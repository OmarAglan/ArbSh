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
    /// <param name="session">حالة الجلسة الحالية (مثل المجلد الحالي).</param>
    public static void ExecuteInput(
        string inputLine,
        IExecutionSink sink,
        ExecutionOptions? options = null,
        ShellSessionState? session = null)
    {
        ArgumentNullException.ThrowIfNull(inputLine);
        ArgumentNullException.ThrowIfNull(sink);

        session ??= new ShellSessionState();

        using var sessionScope = ShellSessionContext.Push(session);
        using var scope = CoreConsole.PushSink(sink, options);
        var commands = Parser.Parse(inputLine);
        Executor.Execute(commands, sink, options);
    }
}
