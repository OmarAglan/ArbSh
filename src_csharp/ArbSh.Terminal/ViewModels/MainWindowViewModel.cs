using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using ArbSh.Core;
using ArbSh.Terminal.Models;

namespace ArbSh.Terminal.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const string ExitCommand = "اخرج";
    private readonly ShellSessionState _session;
    private readonly ObservableCollection<TerminalLine> _lines = [];
    private readonly ReadOnlyObservableCollection<TerminalLine> _readonlyLines;
    private string _statusText = "جاهز";

    public MainWindowViewModel(string? initialWorkingDirectory = null)
    {
        _session = new ShellSessionState(initialWorkingDirectory);
        _readonlyLines = new ReadOnlyObservableCollection<TerminalLine>(_lines);
        AddLine("مرحبًا بك في أربش", TerminalLineKind.System);
        AddLine("صدفة عربية حديثة — اكتب «مساعدة» لعرض الأوامر.", TerminalLineKind.System);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? BufferChanged;
    public event EventHandler? ExitRequested;

    public ReadOnlyObservableCollection<TerminalLine> Lines => _readonlyLines;

    public string CurrentDirectory => _session.CurrentDirectory;

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (string.Equals(_statusText, value, StringComparison.Ordinal))
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

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
            StatusText = "جارٍ التنفيذ…";
            await Task.Run(
                () => ShellEngine.ExecuteInput(
                    logicalInput,
                    sink,
                    options,
                    _session,
                    cancellationToken: cancellationToken),
                cancellationToken);
        }
        catch (Exception ex)
        {
            AddLine($"خطأ: {ex.Message}", TerminalLineKind.Error);
        }
        finally
        {
            OnPropertyChanged(nameof(CurrentDirectory));
            StatusText = "جاهز";
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
