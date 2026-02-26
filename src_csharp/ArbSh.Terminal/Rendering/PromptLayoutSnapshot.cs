using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace ArbSh.Terminal.Rendering;

/// <summary>
/// لقطة تخطيط سطر الموجه المستخدمة لاختبار المواضع ورسم المؤشر/التحديد.
/// Snapshot of prompt line layout used for hit-testing and caret/selection rendering.
/// </summary>
public sealed class PromptLayoutSnapshot
{
    /// <summary>
    /// ينشئ لقطة تخطيط سطر الموجه.
    /// Creates a prompt line layout snapshot.
    /// </summary>
    /// <param name="layout">تخطيط النص.</param>
    /// <param name="origin">موضع الرسم.</param>
    /// <param name="logicalText">النص المنطقي الكامل (Prompt + Input).</param>
    /// <param name="promptLength">طول نص الموجه المنطقي.</param>
    public PromptLayoutSnapshot(TextLayout layout, Point origin, string logicalText, int promptLength)
    {
        Layout = layout;
        Origin = origin;
        LogicalText = logicalText;
        PromptLength = promptLength;
    }

    /// <summary>
    /// تخطيط النص الحالي.
    /// Current text layout.
    /// </summary>
    public TextLayout Layout { get; }

    /// <summary>
    /// نقطة أصل الرسم.
    /// Draw origin.
    /// </summary>
    public Point Origin { get; }

    /// <summary>
    /// النص المنطقي الكامل (Prompt + Input).
    /// Full logical line text (prompt + input).
    /// </summary>
    public string LogicalText { get; }

    /// <summary>
    /// طول الموجه (غير قابل للتحرير).
    /// Non-editable prompt length.
    /// </summary>
    public int PromptLength { get; }

    /// <summary>
    /// طول جزء الإدخال القابل للتحرير.
    /// Editable input length.
    /// </summary>
    public int InputLength => Math.Max(0, LogicalText.Length - PromptLength);

    /// <summary>
    /// يحول موضع مؤشر الإدخال إلى موضع X بصري.
    /// Converts input caret index to visual X coordinate.
    /// </summary>
    /// <param name="inputIndex">موضع الإدخال المنطقي.</param>
    /// <returns>إحداثي X بالنسبة لسطح الرسم.</returns>
    public double GetCaretXForInputIndex(int inputIndex)
    {
        if (Layout.TextLines.Count == 0)
        {
            return Origin.X;
        }

        TextLine line = Layout.TextLines[0];
        int clampedInput = Clamp(inputIndex, 0, InputLength);
        int fullIndex = PromptLength + clampedInput;
        var hit = new CharacterHit(fullIndex, 0);
        double distance = line.GetDistanceFromCharacterHit(hit);
        return Origin.X + distance;
    }

    /// <summary>
    /// يحول نقطة ماوس إلى موضع إدخال منطقي.
    /// Converts pointer position to logical input index.
    /// </summary>
    /// <param name="point">نقطة الماوس على السطح.</param>
    /// <returns>موضع الإدخال المنطقي.</returns>
    public int GetInputIndexFromPoint(Point point)
    {
        if (Layout.TextLines.Count == 0)
        {
            return 0;
        }

        TextLine line = Layout.TextLines[0];
        double distance = point.X - Origin.X;
        CharacterHit hit = line.GetCharacterHitFromDistance(distance);
        int fullIndex = hit.FirstCharacterIndex + Math.Max(0, hit.TrailingLength);
        fullIndex = Clamp(fullIndex, 0, LogicalText.Length);

        return Clamp(fullIndex - PromptLength, 0, InputLength);
    }

    /// <summary>
    /// يرجع مستطيلات التحديد البصرية ضمن جزء الإدخال.
    /// Returns visual selection rectangles for the editable input segment.
    /// </summary>
    /// <param name="inputStart">بداية التحديد المنطقي في الإدخال.</param>
    /// <param name="inputLength">طول التحديد المنطقي.</param>
    /// <returns>قائمة مستطيلات التحديد.</returns>
    public IReadOnlyList<Rect> GetSelectionRects(int inputStart, int inputLength)
    {
        if (Layout.TextLines.Count == 0 || inputLength <= 0)
        {
            return [];
        }

        TextLine line = Layout.TextLines[0];
        int start = Clamp(inputStart, 0, InputLength);
        int length = Clamp(inputLength, 0, InputLength - start);

        int fullStart = PromptLength + start;
        IReadOnlyList<TextBounds> bounds = line.GetTextBounds(fullStart, length);
        var rects = new List<Rect>(bounds.Count);

        foreach (TextBounds bound in bounds)
        {
            Rect r = bound.Rectangle;
            rects.Add(new Rect(Origin.X + r.X, Origin.Y + r.Y, r.Width, r.Height));
        }

        return rects;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(max, Math.Max(min, value));
    }
}
