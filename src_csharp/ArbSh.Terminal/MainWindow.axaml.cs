using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ArbSh.Terminal.Rendering;

namespace ArbSh.Terminal;

public partial class MainWindow : Window
{
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

        var terminal = this.FindControl<TerminalSurface>("TerminalSurface");
        terminal?.Focus();
    }
}
