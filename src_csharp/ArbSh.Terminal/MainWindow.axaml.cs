using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ArbSh.Terminal.Rendering;

namespace ArbSh.Terminal;

public partial class MainWindow : Window
{
    private ViewModels.MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        _viewModel = DataContext as ViewModels.MainWindowViewModel;
        if (_viewModel is not null)
        {
            _viewModel.ExitRequested += HandleExitRequested;
        }

        var terminal = this.FindControl<TerminalSurface>("TerminalSurface");
        terminal?.Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.ExitRequested -= HandleExitRequested;
            _viewModel = null;
        }

        base.OnClosed(e);
    }

    private void HandleExitRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
