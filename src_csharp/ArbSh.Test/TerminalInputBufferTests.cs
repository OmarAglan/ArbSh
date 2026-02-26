using ArbSh.Terminal.Input;

namespace ArbSh.Test;

public sealed class TerminalInputBufferTests
{
    [Fact]
    public void InsertText_InsertsAtCaretAndMovesCaret()
    {
        var buffer = new TerminalInputBuffer();

        buffer.InsertText("abc");
        buffer.SetCaretFromLogicalIndex(1);
        buffer.InsertText("X");

        Assert.Equal("aXbc", buffer.Text);
        Assert.Equal(2, buffer.CaretIndex);
    }

    [Fact]
    public void Backspace_DeletesWholeTextElement_ForCombiningSequence()
    {
        var buffer = new TerminalInputBuffer();
        buffer.InsertText("ا\u0651");

        buffer.Backspace();

        Assert.Equal(string.Empty, buffer.Text);
        Assert.Equal(0, buffer.CaretIndex);
    }

    [Fact]
    public void DeleteForward_DeletesWholeTextElement_ForCombiningSequence()
    {
        var buffer = new TerminalInputBuffer();
        buffer.InsertText("ا\u0651ب");
        buffer.MoveCaretHome(false);

        buffer.DeleteForward();

        Assert.Equal("ب", buffer.Text);
        Assert.Equal(0, buffer.CaretIndex);
    }

    [Fact]
    public void ReplaceSelection_WhenInsertingText()
    {
        var buffer = new TerminalInputBuffer();
        buffer.InsertText("abcdef");
        buffer.SetCaretFromLogicalIndex(2);
        buffer.SetCaretFromLogicalIndex(5, extendSelection: true);

        buffer.InsertText("X");

        Assert.Equal("abXf", buffer.Text);
        Assert.Equal(3, buffer.CaretIndex);
        Assert.False(buffer.HasSelection);
    }

    [Fact]
    public void SelectAll_AndDeleteForward_ClearsText()
    {
        var buffer = new TerminalInputBuffer();
        buffer.InsertText("مرحبا");
        buffer.SelectAll();

        buffer.DeleteForward();

        Assert.Equal(string.Empty, buffer.Text);
        Assert.Equal(0, buffer.CaretIndex);
    }

    [Fact]
    public void MoveHomeEnd_WithExtendSelection_CreatesRange()
    {
        var buffer = new TerminalInputBuffer();
        buffer.InsertText("hello");
        buffer.MoveCaretHome(false);

        buffer.MoveCaretEnd(extendSelection: true);

        SelectionRange selection = Assert.NotNull(buffer.Selection).Value;
        Assert.Equal(0, selection.Start);
        Assert.Equal(5, selection.End);
    }
}
