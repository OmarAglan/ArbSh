using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using ArbSh.Core.I18n;
using ArbSh.Terminal.Input;
using ArbSh.Terminal.Models;
using ArbSh.Terminal.ViewModels;

namespace ArbSh.Terminal.Rendering;

public sealed class TerminalSurface : Control
{
    private const double CaretDistanceEpsilon = 0.01;

    private MainWindowViewModel? _viewModel;
    private bool _isPointerSelecting;

    private readonly TerminalInputBuffer _inputBuffer = new();
    private readonly TerminalRenderConfig _renderConfig = new();
    private readonly TerminalTextPipeline _textPipeline = new();
    private readonly TerminalLayoutEngine _layoutEngine = new();

    private PromptLayoutSnapshot? _promptSnapshot;

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

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            bool extendSelection = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            UpdateCaretFromPointer(e.GetPosition(this), extendSelection);

            _isPointerSelecting = true;
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_isPointerSelecting && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            UpdateCaretFromPointer(e.GetPosition(this), extendSelection: true);
            e.Handled = true;
        }

        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isPointerSelecting)
        {
            _isPointerSelecting = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        base.OnPointerReleased(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        bool hasPrintableChar = e.Text.Any(c => !char.IsControl(c));
        if (!hasPrintableChar)
        {
            return;
        }

        _inputBuffer.InsertText(e.Text);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        if (ctrl)
        {
            switch (e.Key)
            {
                case Key.A:
                    _inputBuffer.SelectAll();
                    InvalidateVisual();
                    e.Handled = true;
                    return;

                case Key.C:
                    await CopySelectionAsync();
                    e.Handled = true;
                    return;

                case Key.X:
                    await CutSelectionAsync();
                    InvalidateVisual();
                    e.Handled = true;
                    return;

                case Key.V:
                    await PasteClipboardAsync();
                    InvalidateVisual();
                    e.Handled = true;
                    return;
            }
        }

        switch (e.Key)
        {
            case Key.Left:
                MoveCaretVisual(moveLeft: true, extendSelection: shift);
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Right:
                MoveCaretVisual(moveLeft: false, extendSelection: shift);
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Home:
                _inputBuffer.MoveCaretHome(shift);
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.End:
                _inputBuffer.MoveCaretEnd(shift);
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Back:
                _inputBuffer.Backspace();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Delete:
                _inputBuffer.DeleteForward();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Escape:
                _inputBuffer.ClearSelection();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Enter:
                await SubmitInputBufferAsync();
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
            _inputBuffer.Text,
            Bounds.Size,
            _renderConfig,
            _textPipeline);

        _promptSnapshot = null;

        foreach (TerminalDrawInstruction instruction in instructions)
        {
            if (instruction.IsPromptLine)
            {
                DrawPromptLine(context, instruction);
                continue;
            }

            FormattedText formatted = _renderConfig.CreateFormattedText(instruction.Run.VisualText, instruction.Brush);
            context.DrawText(formatted, instruction.Position);
        }

        if (IsFocused && _promptSnapshot is not null)
        {
            DrawCaret(context, _promptSnapshot);
        }
    }

    private void DrawPromptLine(DrawingContext context, TerminalDrawInstruction instruction)
    {
        if (_viewModel is null)
        {
            return;
        }

        bool isPromptRtl = IsTextRtl(instruction.Run.LogicalText);
        FlowDirection flow = isPromptRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        TextLayout layout = _renderConfig.CreateTextLayout(instruction.Run.LogicalText, instruction.Brush, flow);
        var snapshot = new PromptLayoutSnapshot(layout, instruction.Position, instruction.Run.LogicalText, _viewModel.Prompt.Length);
        _promptSnapshot = snapshot;

        DrawSelection(context, snapshot);
        layout.Draw(context, instruction.Position);
    }

    private void DrawSelection(DrawingContext context, PromptLayoutSnapshot snapshot)
    {
        if (_inputBuffer.Selection is not { HasSelection: true } selection)
        {
            return;
        }

        IReadOnlyList<Rect> rects = snapshot.GetSelectionRects(selection.Start, selection.Length);
        foreach (Rect rect in rects)
        {
            context.DrawRectangle(_renderConfig.SelectionBrush, null, rect);
        }
    }

    private void DrawCaret(DrawingContext context, PromptLayoutSnapshot snapshot)
    {
        double cursorX = snapshot.GetCaretXForInputIndex(_inputBuffer.CaretIndex);

        double minCursorX = _renderConfig.Padding.Left;
        double maxCursorX = Math.Max(minCursorX, Bounds.Width - _renderConfig.Padding.Right);
        cursorX = Math.Clamp(cursorX, minCursorX, maxCursorX);

        double cursorY = snapshot.Origin.Y + 2;
        var cursorRect = new Rect(cursorX, cursorY, 2, Math.Max(6, _renderConfig.LineHeight - 6));
        context.DrawRectangle(_renderConfig.PromptBrush, null, cursorRect);
    }

    private async Task SubmitInputBufferAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        string input = _inputBuffer.Text;
        _inputBuffer.Clear();
        InvalidateVisual();

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        await _viewModel.SubmitInputAsync(input);
    }

    private void MoveCaretVisual(bool moveLeft, bool extendSelection)
    {
        if (!TryGetPromptSnapshot(out PromptLayoutSnapshot snapshot) || snapshot.Layout.TextLines.Count == 0)
        {
            if (moveLeft)
            {
                _inputBuffer.MoveCaretLeft(extendSelection);
            }
            else
            {
                _inputBuffer.MoveCaretRight(extendSelection);
            }

            return;
        }

        TextLine line = snapshot.Layout.TextLines[0];

        int minFull = snapshot.PromptLength;
        int maxFull = snapshot.PromptLength + _inputBuffer.Text.Length;
        int fullIndex = Math.Clamp(snapshot.PromptLength + _inputBuffer.CaretIndex, minFull, maxFull);

        var currentHit = new CharacterHit(fullIndex, 0);
        double currentDistance = line.GetDistanceFromCharacterHit(currentHit);

        CharacterHit previousHit = line.GetPreviousCaretCharacterHit(currentHit);
        CharacterHit nextHit = line.GetNextCaretCharacterHit(currentHit);

        int previousIndex = Math.Clamp(ToFullIndex(previousHit), minFull, maxFull);
        int nextIndex = Math.Clamp(ToFullIndex(nextHit), minFull, maxFull);

        double previousDistance = line.GetDistanceFromCharacterHit(previousHit);
        double nextDistance = line.GetDistanceFromCharacterHit(nextHit);

        int targetFullIndex = fullIndex;

        if (moveLeft)
        {
            bool previousIsLeft = previousDistance < currentDistance - CaretDistanceEpsilon;
            bool nextIsLeft = nextDistance < currentDistance - CaretDistanceEpsilon;

            if (previousIsLeft && nextIsLeft)
            {
                targetFullIndex = previousDistance > nextDistance ? previousIndex : nextIndex;
            }
            else if (previousIsLeft)
            {
                targetFullIndex = previousIndex;
            }
            else if (nextIsLeft)
            {
                targetFullIndex = nextIndex;
            }
            else
            {
                if (previousIndex < fullIndex || nextIndex < fullIndex)
                {
                    int candidate = fullIndex;
                    if (previousIndex < fullIndex)
                    {
                        candidate = previousIndex;
                    }

                    if (nextIndex < fullIndex)
                    {
                        candidate = Math.Max(candidate, nextIndex);
                    }

                    targetFullIndex = candidate;
                }
            }
        }
        else
        {
            bool previousIsRight = previousDistance > currentDistance + CaretDistanceEpsilon;
            bool nextIsRight = nextDistance > currentDistance + CaretDistanceEpsilon;

            if (previousIsRight && nextIsRight)
            {
                targetFullIndex = previousDistance < nextDistance ? previousIndex : nextIndex;
            }
            else if (previousIsRight)
            {
                targetFullIndex = previousIndex;
            }
            else if (nextIsRight)
            {
                targetFullIndex = nextIndex;
            }
            else
            {
                if (previousIndex > fullIndex || nextIndex > fullIndex)
                {
                    int candidate = fullIndex;
                    if (previousIndex > fullIndex)
                    {
                        candidate = previousIndex;
                    }

                    if (nextIndex > fullIndex)
                    {
                        candidate = Math.Min(candidate, nextIndex);
                    }

                    targetFullIndex = candidate;
                }
            }
        }

        if (targetFullIndex == fullIndex)
        {
            if (moveLeft)
            {
                _inputBuffer.MoveCaretLeft(extendSelection);
            }
            else
            {
                _inputBuffer.MoveCaretRight(extendSelection);
            }

            return;
        }

        int targetInputIndex = Math.Clamp(targetFullIndex - snapshot.PromptLength, 0, _inputBuffer.Text.Length);
        _inputBuffer.SetCaretFromLogicalIndex(targetInputIndex, extendSelection);
    }

    private void UpdateCaretFromPointer(Point point, bool extendSelection)
    {
        if (!TryGetPromptSnapshot(out PromptLayoutSnapshot snapshot) || !IsPointOnPromptLine(point, snapshot))
        {
            return;
        }

        int inputIndex = snapshot.GetInputIndexFromPoint(point);
        _inputBuffer.SetCaretFromLogicalIndex(inputIndex, extendSelection);
        InvalidateVisual();
    }

    private bool TryGetPromptSnapshot(out PromptLayoutSnapshot snapshot)
    {
        snapshot = null!;

        if (_viewModel is null)
        {
            return false;
        }

        string expectedText = string.Concat(_viewModel.Prompt, _inputBuffer.Text);
        if (_promptSnapshot is not null && _promptSnapshot.LogicalText == expectedText)
        {
            snapshot = _promptSnapshot;
            return true;
        }

        IReadOnlyList<TerminalLine> lineSnapshot = [.. _viewModel.Lines];
        IReadOnlyList<TerminalDrawInstruction> instructions = _layoutEngine.BuildFrame(
            lineSnapshot,
            _viewModel.Prompt,
            _inputBuffer.Text,
            Bounds.Size,
            _renderConfig,
            _textPipeline);

        TerminalDrawInstruction? promptInstruction = instructions.FirstOrDefault(x => x.IsPromptLine);
        if (promptInstruction is null)
        {
            return false;
        }

        bool isPromptRtl = IsTextRtl(promptInstruction.Run.LogicalText);
        FlowDirection flow = isPromptRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        TextLayout layout = _renderConfig.CreateTextLayout(promptInstruction.Run.LogicalText, promptInstruction.Brush, flow);
        _promptSnapshot = new PromptLayoutSnapshot(layout, promptInstruction.Position, promptInstruction.Run.LogicalText, _viewModel.Prompt.Length);

        snapshot = _promptSnapshot;
        return true;
    }

    private bool IsPointOnPromptLine(Point point, PromptLayoutSnapshot snapshot)
    {
        double top = snapshot.Origin.Y;
        double bottom = top + _renderConfig.LineHeight;
        return point.Y >= top && point.Y <= bottom;
    }

    private async Task CopySelectionAsync()
    {
        if (!_inputBuffer.HasSelection)
        {
            return;
        }

        string selected = _inputBuffer.GetSelectedText();
        if (string.IsNullOrEmpty(selected))
        {
            return;
        }

        IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        await clipboard.SetTextAsync(selected);
    }

    private async Task CutSelectionAsync()
    {
        await CopySelectionAsync();
        _inputBuffer.DeleteSelectionIfAny();
    }

    private async Task PasteClipboardAsync()
    {
        IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        string? text = await clipboard.GetTextAsync();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _inputBuffer.InsertText(text);
    }

    private static int ToFullIndex(CharacterHit hit)
    {
        return hit.FirstCharacterIndex + Math.Max(0, hit.TrailingLength);
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
