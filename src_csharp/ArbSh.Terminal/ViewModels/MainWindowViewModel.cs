using System.Collections.ObjectModel;
using Avalonia.Threading;
using ArbSh.Core;
using ArbSh.Terminal.Models;

namespace ArbSh.Terminal.ViewModels;

public sealed class MainWindowViewModel
{
    private const string ExitCommand = "اخرج";
    private readonly ObservableCollection<TerminalLine> _lines = [];
    private readonly ReadOnlyObservableCollection<TerminalLine> _readonlyLines;

    public MainWindowViewModel()
    {
        _readonlyLines = new ReadOnlyObservableCollection<TerminalLine>(_lines);
        AddLine("مرحباً بكم في أربش - الواجهة الرسومية قيد البناء.", TerminalLineKind.System);
        AddLine("اكتب أمرًا واضغط Enter للتنفيذ.", TerminalLineKind.System);
        AddLine("للخروج من الواجهة اكتب: اخرج", TerminalLineKind.System);
    }

    public event EventHandler? BufferChanged;
    public event EventHandler? ExitRequested;

    public ReadOnlyObservableCollection<TerminalLine> Lines => _readonlyLines;

    public string Prompt { get; } = "أربش> ";

    public async Task SubmitInputAsync(string logicalInput, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(logicalInput))
        {
            return;
        }

        AddLine(logicalInput, TerminalLineKind.Input);

        string trimmedInput = logicalInput.Trim();
        if (string.Equals(trimmedInput, ExitCommand, StringComparison.Ordinal))
        {
            Dispatcher.UIThread.Post(() => ExitRequested?.Invoke(this, EventArgs.Empty));
            return;
        }

        var sink = new TerminalExecutionSink(this);
        var options = new ExecutionOptions
        {
            EmitDebug = false
        };

        try
        {
            await Task.Run(() => ShellEngine.ExecuteInput(logicalInput, sink, options), cancellationToken);
        }
        catch (Exception ex)
        {
            AddLine($"ERROR: {ex.Message}", TerminalLineKind.Error);
        }
    }

    internal void PostLine(string message, TerminalLineKind kind)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Dispatcher.UIThread.Post(() => AddLine(message, kind));
    }

    private void AddLine(string text, TerminalLineKind kind)
    {
        _lines.Add(new TerminalLine(text, kind, DateTimeOffset.UtcNow));

        const int maxLines = 5000;
        while (_lines.Count > maxLines)
        {
            _lines.RemoveAt(0);
        }

        BufferChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class TerminalExecutionSink : IExecutionSink
    {
        private readonly MainWindowViewModel _owner;

        public TerminalExecutionSink(MainWindowViewModel owner)
        {
            _owner = owner;
        }

        public void WriteOutput(string message)
        {
            _owner.PostLine(message, TerminalLineKind.Output);
        }

        public void WriteError(string message)
        {
            _owner.PostLine(message, TerminalLineKind.Error);
        }

        public void WriteWarning(string message)
        {
            _owner.PostLine(message, TerminalLineKind.Warning);
        }

        public void WriteDebug(string message)
        {
            _owner.PostLine(message, TerminalLineKind.Debug);
        }
    }
}
