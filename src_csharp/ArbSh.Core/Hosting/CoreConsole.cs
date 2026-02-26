using System.IO;
using System.Text;
using System.Threading;

namespace ArbSh.Core;

/// <summary>
/// Internal console abstraction for host-agnostic engine diagnostics and stream routing.
/// </summary>
internal static class CoreConsole
{
    private static readonly AsyncLocal<ExecutionContext?> CurrentContext = new();
    private static readonly SinkTextWriter StdOutWriter = new(isError: false);
    private static readonly SinkTextWriter StdErrWriter = new(isError: true);
    private static ConsoleColor _foregroundColor = ConsoleColor.Gray;

    /// <summary>
    /// Gets or sets the current foreground color.
    /// </summary>
    public static ConsoleColor ForegroundColor
    {
        get => _foregroundColor;
        set => _foregroundColor = value;
    }

    /// <summary>
    /// Gets a text writer that routes output lines to the active sink as stdout.
    /// </summary>
    public static TextWriter Out => StdOutWriter;

    /// <summary>
    /// Gets a text writer that routes output lines to the active sink as stderr.
    /// </summary>
    public static TextWriter Error => StdErrWriter;

    /// <summary>
    /// Begins an execution sink scope for current async flow.
    /// </summary>
    /// <param name="sink">The sink instance to receive output.</param>
    /// <param name="options">Optional execution behavior flags.</param>
    /// <returns>A disposable scope that restores previous sink context.</returns>
    public static IDisposable PushSink(IExecutionSink? sink, ExecutionOptions? options = null)
    {
        var previous = CurrentContext.Value;
        CurrentContext.Value = new ExecutionContext(sink ?? NullExecutionSink.Instance, options ?? new ExecutionOptions());
        return new Scope(() => CurrentContext.Value = previous);
    }

    /// <summary>
    /// Writes a line using sink classification rules.
    /// </summary>
    /// <param name="message">The message to emit.</param>
    public static void WriteLine(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        var context = CurrentContext.Value;
        if (context is null)
        {
            return;
        }

        if (IsDebug(message))
        {
            if (context.Options.EmitDebug)
            {
                context.Sink.WriteDebug(message);
            }
            return;
        }

        if (IsWarning(message))
        {
            if (context.Options.EmitWarnings)
            {
                context.Sink.WriteWarning(message);
            }
            return;
        }

        if (IsError(message))
        {
            context.Sink.WriteError(message);
            return;
        }

        context.Sink.WriteOutput(message);
    }

    /// <summary>
    /// Writes raw text to stdout sink without line classification.
    /// </summary>
    /// <param name="message">The message to emit.</param>
    public static void Write(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        var context = CurrentContext.Value;
        context?.Sink.WriteOutput(message);
    }

    /// <summary>
    /// Resets color state. This is a no-op in host-agnostic mode.
    /// </summary>
    public static void ResetColor()
    {
        _foregroundColor = ConsoleColor.Gray;
    }

    private static bool IsDebug(string message)
    {
        return message.StartsWith("DEBUG", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWarning(string message)
    {
        return message.StartsWith("WARN", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("WARNING", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsError(string message)
    {
        return message.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Task Error", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("[ERROR", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ExecutionContext
    {
        public ExecutionContext(IExecutionSink sink, ExecutionOptions options)
        {
            Sink = sink;
            Options = options;
        }

        public IExecutionSink Sink { get; }

        public ExecutionOptions Options { get; }
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public Scope(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _onDispose();
        }
    }

    private sealed class SinkTextWriter : TextWriter
    {
        private readonly bool _isError;

        public SinkTextWriter(bool isError)
        {
            _isError = isError;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            var context = CurrentContext.Value;
            if (context is null)
            {
                return;
            }

            if (_isError)
            {
                context.Sink.WriteError(value);
                return;
            }

            context.Sink.WriteOutput(value);
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            var context = CurrentContext.Value;
            if (context is null)
            {
                return;
            }

            if (_isError)
            {
                context.Sink.WriteError(value);
                return;
            }

            context.Sink.WriteOutput(value);
        }
    }
}
