using System.Globalization;

namespace ArbSh.Terminal.Input;

/// <summary>
/// يمثل مخزن إدخال منطقي مع موضع مؤشر وتحديد.
/// Represents a logical input buffer with caret and selection state.
/// </summary>
public sealed class TerminalInputBuffer
{
    private string _text = string.Empty;
    private int _caretIndex;
    private int? _selectionAnchor;
    private SelectionRange? _selection;

    /// <summary>
    /// النص المنطقي الحالي للمستخدم.
    /// Current logical input text.
    /// </summary>
    public string Text => _text;

    /// <summary>
    /// موضع المؤشر المنطقي الحالي.
    /// Current logical caret index.
    /// </summary>
    public int CaretIndex => _caretIndex;

    /// <summary>
    /// التحديد المنطقي الحالي إن وُجد.
    /// Current logical selection when present.
    /// </summary>
    public SelectionRange? Selection => _selection;

    /// <summary>
    /// هل يوجد تحديد نشط.
    /// Indicates whether there is an active selection.
    /// </summary>
    public bool HasSelection => _selection is { HasSelection: true };

    /// <summary>
    /// يعيد المخزن إلى الحالة الابتدائية.
    /// Resets the buffer to initial empty state.
    /// </summary>
    public void Clear()
    {
        _text = string.Empty;
        _caretIndex = 0;
        _selection = null;
        _selectionAnchor = null;
    }

    /// <summary>
    /// يحدد المؤشر عند موقع منطقي مع دعم تمديد التحديد.
    /// Sets caret at a logical index with optional selection extension.
    /// </summary>
    /// <param name="index">الموضع المطلوب.</param>
    /// <param name="extendSelection">تمديد التحديد الحالي.</param>
    public void SetCaretFromLogicalIndex(int index, bool extendSelection = false)
    {
        int clamped = Clamp(index, 0, _text.Length);

        if (extendSelection)
        {
            _selectionAnchor ??= _caretIndex;
            _selection = new SelectionRange(_selectionAnchor.Value, clamped);
        }
        else
        {
            _selection = null;
            _selectionAnchor = null;
        }

        _caretIndex = clamped;
    }

    /// <summary>
    /// يحرك المؤشر عنصرًا نصيًا واحدًا للخلف (منطقيًا).
    /// Moves caret one text element backward (logical).
    /// </summary>
    /// <param name="extendSelection">تمديد التحديد الحالي.</param>
    public void MoveCaretLeft(bool extendSelection)
    {
        int previous = GetPreviousTextElementIndex(_text, _caretIndex);
        SetCaretFromLogicalIndex(previous, extendSelection);
    }

    /// <summary>
    /// يحرك المؤشر عنصرًا نصيًا واحدًا للأمام (منطقيًا).
    /// Moves caret one text element forward (logical).
    /// </summary>
    /// <param name="extendSelection">تمديد التحديد الحالي.</param>
    public void MoveCaretRight(bool extendSelection)
    {
        int next = GetNextTextElementIndex(_text, _caretIndex);
        SetCaretFromLogicalIndex(next, extendSelection);
    }

    /// <summary>
    /// ينقل المؤشر إلى بداية النص.
    /// Moves caret to start of input.
    /// </summary>
    /// <param name="extendSelection">تمديد التحديد الحالي.</param>
    public void MoveCaretHome(bool extendSelection)
    {
        SetCaretFromLogicalIndex(0, extendSelection);
    }

    /// <summary>
    /// ينقل المؤشر إلى نهاية النص.
    /// Moves caret to end of input.
    /// </summary>
    /// <param name="extendSelection">تمديد التحديد الحالي.</param>
    public void MoveCaretEnd(bool extendSelection)
    {
        SetCaretFromLogicalIndex(_text.Length, extendSelection);
    }

    /// <summary>
    /// يدرج نصًا عند المؤشر (ويستبدل التحديد إن وجد).
    /// Inserts text at caret (replacing selection when present).
    /// </summary>
    /// <param name="text">النص المراد إدراجه.</param>
    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        DeleteSelectionIfAny();
        _text = _text.Insert(_caretIndex, text);
        _caretIndex += text.Length;
        _selection = null;
        _selectionAnchor = null;
    }

    /// <summary>
    /// يحذف عنصرًا نصيًا قبل المؤشر أو التحديد النشط.
    /// Deletes one text element before caret or active selection.
    /// </summary>
    public void Backspace()
    {
        if (DeleteSelectionIfAny())
        {
            return;
        }

        if (_caretIndex <= 0)
        {
            return;
        }

        int previous = GetPreviousTextElementIndex(_text, _caretIndex);
        int length = _caretIndex - previous;

        _text = _text.Remove(previous, length);
        _caretIndex = previous;
        _selection = null;
        _selectionAnchor = null;
    }

    /// <summary>
    /// يحذف عنصرًا نصيًا بعد المؤشر أو التحديد النشط.
    /// Deletes one text element after caret or active selection.
    /// </summary>
    public void DeleteForward()
    {
        if (DeleteSelectionIfAny())
        {
            return;
        }

        if (_caretIndex >= _text.Length)
        {
            return;
        }

        int next = GetNextTextElementIndex(_text, _caretIndex);
        int length = next - _caretIndex;

        _text = _text.Remove(_caretIndex, length);
        _selection = null;
        _selectionAnchor = null;
    }

    /// <summary>
    /// يحدد كل النص الحالي.
    /// Selects all input text.
    /// </summary>
    public void SelectAll()
    {
        _selectionAnchor = 0;
        _selection = new SelectionRange(0, _text.Length);
        _caretIndex = _text.Length;
    }

    /// <summary>
    /// يلغي التحديد الحالي.
    /// Clears current selection.
    /// </summary>
    public void ClearSelection()
    {
        _selection = null;
        _selectionAnchor = null;
    }

    /// <summary>
    /// يحذف التحديد الحالي إن وجد.
    /// Deletes active selection if present.
    /// </summary>
    /// <returns>صحيح إذا تم حذف تحديد.</returns>
    public bool DeleteSelectionIfAny()
    {
        if (_selection is not { HasSelection: true } selection)
        {
            return false;
        }

        int start = selection.Start;
        int length = selection.Length;

        _text = _text.Remove(start, length);
        _caretIndex = start;
        _selection = null;
        _selectionAnchor = null;
        return true;
    }

    /// <summary>
    /// يرجع النص المحدد حاليًا.
    /// Returns currently selected text.
    /// </summary>
    /// <returns>النص المحدد أو سلسلة فارغة.</returns>
    public string GetSelectedText()
    {
        if (_selection is not { HasSelection: true } selection)
        {
            return string.Empty;
        }

        return _text.Substring(selection.Start, selection.Length);
    }

    private static int GetPreviousTextElementIndex(string text, int index)
    {
        if (index <= 0 || string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int[] elements = StringInfo.ParseCombiningCharacters(text);
        int best = 0;

        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i] >= index)
            {
                break;
            }

            best = elements[i];
        }

        return best;
    }

    private static int GetNextTextElementIndex(string text, int index)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        if (index >= text.Length)
        {
            return text.Length;
        }

        int[] elements = StringInfo.ParseCombiningCharacters(text);
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i] > index)
            {
                return elements[i];
            }
        }

        return text.Length;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(max, Math.Max(min, value));
    }
}
