using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using ArbSh.Core.I18n;
using ArbSh.Terminal.Models;
using ArbSh.Terminal.ViewModels;

namespace ArbSh.Terminal.Rendering;

public sealed class TerminalSurface : Control
{
    private string _inputBuffer = string.Empty;
    private MainWindowViewModel? _viewModel;

    private readonly TerminalRenderConfig _renderConfig = new();
    private readonly TerminalTextPipeline _textPipeline = new();
    private readonly TerminalLayoutEngine _layoutEngine = new();

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

        context.DrawRectangle(_renderConfig.BackgroundBrush, null, new Rect(Bounds.Size));

        if (_viewModel is null)
        {
            return;
        }

        IReadOnlyList<TerminalLine> lineSnapshot = [.. _viewModel.Lines];
        IReadOnlyList<TerminalDrawInstruction> instructions = _layoutEngine.BuildFrame(
            lineSnapshot,
            _viewModel.Prompt,
            _inputBuffer,
            Bounds.Size,
            _renderConfig,
            _textPipeline);

        TerminalDrawInstruction? promptInstruction = null;
        TextLayout? promptLayout = null;

        foreach (TerminalDrawInstruction instruction in instructions)
        {
            if (instruction.IsPromptLine)
            {
                promptInstruction = instruction;
                bool isPromptRtl = IsTextRtl(instruction.Run.LogicalText);
                FlowDirection flow = isPromptRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                promptLayout = _renderConfig.CreateTextLayout(instruction.Run.LogicalText, instruction.Brush, flow);
                promptLayout.Draw(context, instruction.Position);
                continue;
            }

            FormattedText formatted = _renderConfig.CreateFormattedText(instruction.Run.VisualText, instruction.Brush);
            context.DrawText(formatted, instruction.Position);
        }

        if (IsFocused && promptInstruction is not null)
        {
            DrawCursor(context, promptInstruction, promptLayout);
        }
    }

    private void DrawCursor(DrawingContext context, TerminalDrawInstruction promptInstruction, TextLayout? promptLayout)
    {
        double cursorX = promptInstruction.Position.X + promptInstruction.Run.MeasuredWidth + 1;

        if (promptLayout is not null && promptLayout.TextLines.Count > 0)
        {
            TextLine line = promptLayout.TextLines[0];
            var endHit = new CharacterHit(promptInstruction.Run.LogicalText.Length, 0);
            double distance = line.GetDistanceFromCharacterHit(endHit);
            cursorX = promptInstruction.Position.X + distance;
        }

        double minCursorX = _renderConfig.Padding.Left;
        double maxCursorX = Math.Max(minCursorX, Bounds.Width - _renderConfig.Padding.Right);
        cursorX = Math.Clamp(cursorX, minCursorX, maxCursorX);

        double cursorY = promptInstruction.Position.Y + 2;
        var cursorRect = new Rect(cursorX, cursorY, 2, Math.Max(6, _renderConfig.LineHeight - 6));
        context.DrawRectangle(_renderConfig.PromptBrush, null, cursorRect);
    }

    private static bool IsTextRtl(string logical)
    {
        if (string.IsNullOrWhiteSpace(logical))
        {
            return false;
        }

        try
        {
            var runs = BidiAlgorithm.ProcessRuns(logical, -1);
            return runs.Count > 0 && (runs[0].Level % 2 != 0);
        }
        catch
        {
            return false;
        }
    }

    private void HandleBufferChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }
}
