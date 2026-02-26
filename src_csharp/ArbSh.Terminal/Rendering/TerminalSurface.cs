using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ArbSh.Core.I18n;
using ArbSh.Terminal.Models;
using ArbSh.Terminal.ViewModels;

namespace ArbSh.Terminal.Rendering;

public sealed class TerminalSurface : Control
{
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#10141F"));
    private static readonly FontFamily TerminalFamily = new("Cascadia Mono, Consolas, Courier New");
    private static readonly Typeface TerminalTypeface = new(TerminalFamily, FontStyle.Normal, FontWeight.Normal);

    private string _inputBuffer = string.Empty;
    private MainWindowViewModel? _viewModel;

    public TerminalSurface()
    {
        Focusable = true;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
        {
            _viewModel.BufferChanged -= HandleBufferChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is not null)
        {
            _viewModel.BufferChanged += HandleBufferChanged;
        }

        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        base.OnPointerPressed(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        _inputBuffer += e.Text;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Back:
                if (_inputBuffer.Length > 0)
                {
                    _inputBuffer = _inputBuffer[..^1];
                    InvalidateVisual();
                }

                e.Handled = true;
                break;

            case Key.Enter:
                var input = _inputBuffer;
                _inputBuffer = string.Empty;
                InvalidateVisual();

                if (_viewModel is not null)
                {
                    await _viewModel.SubmitInputAsync(input);
                }

                e.Handled = true;
                break;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.DrawRectangle(BackgroundBrush, null, new Rect(Bounds.Size));

        if (_viewModel is null)
        {
            return;
        }

        const double horizontalPadding = 14;
        const double verticalPadding = 10;
        const double lineHeight = 22;

        var lines = _viewModel.Lines;
        int maxVisibleLines = Math.Max(1, (int)((Bounds.Height - (verticalPadding * 2) - (lineHeight * 2)) / lineHeight));
        int start = Math.Max(0, lines.Count - maxVisibleLines);

        double y = verticalPadding;
        for (int i = start; i < lines.Count; i++)
        {
            DrawLine(context, lines[i], y, horizontalPadding, Bounds.Width);
            y += lineHeight;
        }

        DrawPrompt(context, _viewModel.Prompt, _inputBuffer, Bounds.Height - verticalPadding - lineHeight, horizontalPadding, lineHeight);
    }

    private static void DrawLine(
        DrawingContext context,
        TerminalLine line,
        double y,
        double horizontalPadding,
        double surfaceWidth)
    {
        string visualText = ToVisual(line.Text);
        IBrush brush = ResolveBrush(line.Kind);

        var formatted = CreateText(visualText, brush);

        double x = horizontalPadding;
        if (BiDiTextProcessor.ContainsArabicText(line.Text))
        {
            x = Math.Max(horizontalPadding, surfaceWidth - horizontalPadding - formatted.Width);
        }

        context.DrawText(formatted, new Point(x, y));
    }

    private void DrawPrompt(
        DrawingContext context,
        string prompt,
        string input,
        double y,
        double horizontalPadding,
        double lineHeight)
    {
        string logical = $"{prompt}{input}";
        string visual = ToVisual(logical);

        var formatted = CreateText(visual, Brushes.Gainsboro);
        double x = Math.Max(horizontalPadding, Bounds.Width - horizontalPadding - formatted.Width);

        context.DrawText(formatted, new Point(x, y));

        if (IsFocused)
        {
            double cursorX = x + formatted.Width + 1;
            var cursorRect = new Rect(cursorX, y + 2, 2, Math.Max(6, lineHeight - 6));
            context.DrawRectangle(Brushes.Gainsboro, null, cursorRect);
        }
    }

    private static FormattedText CreateText(string text, IBrush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            TerminalTypeface,
            15,
            brush);
    }

    private static string ToVisual(string logical)
    {
        if (!BiDiTextProcessor.ContainsArabicText(logical))
        {
            return logical;
        }

        return BiDiTextProcessor.ProcessOutputForDisplay(logical);
    }

    private static IBrush ResolveBrush(TerminalLineKind kind)
    {
        return kind switch
        {
            TerminalLineKind.System => Brushes.LightSteelBlue,
            TerminalLineKind.Input => Brushes.DeepSkyBlue,
            TerminalLineKind.Warning => Brushes.Goldenrod,
            TerminalLineKind.Error => Brushes.IndianRed,
            TerminalLineKind.Debug => Brushes.Gray,
            _ => Brushes.Gainsboro
        };
    }

    private void HandleBufferChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }
}
