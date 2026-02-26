namespace ArbSh.Terminal.Models;

public enum TerminalLineKind
{
    System,
    Input,
    Output,
    Warning,
    Error,
    Debug
}

public sealed record TerminalLine(string Text, TerminalLineKind Kind, DateTimeOffset Timestamp);
