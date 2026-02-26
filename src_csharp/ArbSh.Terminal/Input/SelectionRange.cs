namespace ArbSh.Terminal.Input;

/// <summary>
/// يمثل نطاق تحديد منطقي داخل مخزن الإدخال.
/// Represents a logical selection range inside the input buffer.
/// </summary>
/// <param name="Anchor">موضع بداية التحديد.</param>
/// <param name="Active">موضع نهاية التحديد الحالية.</param>
public readonly record struct SelectionRange(int Anchor, int Active)
{
    /// <summary>
    /// أقل حد في نطاق التحديد (شامل).
    /// The inclusive minimum logical index of the selection.
    /// </summary>
    public int Start => Math.Min(Anchor, Active);

    /// <summary>
    /// أعلى حد في نطاق التحديد (غير شامل).
    /// The exclusive maximum logical index of the selection.
    /// </summary>
    public int End => Math.Max(Anchor, Active);

    /// <summary>
    /// طول التحديد المنطقي.
    /// The logical selection length.
    /// </summary>
    public int Length => End - Start;

    /// <summary>
    /// هل النطاق يحتوي على تحديد فعلي.
    /// Indicates whether the range contains an active selection.
    /// </summary>
    public bool HasSelection => Length > 0;
}
