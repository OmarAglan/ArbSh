namespace ArbSh.Terminal.Rendering;

/// <summary>
/// يمثل ناتج تخطيط إطار طرفية واحد.
/// Represents the computed layout for one terminal frame.
/// </summary>
/// <param name="Instructions">تعليمات الرسم المحسوبة.</param>
/// <param name="FirstVisibleOutputLineIndex">فهرس أول سطر مخرجات ظاهر.</param>
/// <param name="VisibleOutputLineCount">عدد أسطر المخرجات الظاهرة.</param>
/// <param name="MaxVisibleOutputLines">الحد الأقصى لأسطر المخرجات الممكن عرضها في الإطار.</param>
/// <param name="ScrollbackOffsetLines">الإزاحة الحالية عن ذيل المخرجات.</param>
/// <param name="MaxScrollbackOffsetLines">أقصى إزاحة مسموحة ضمن حجم المخرجات.</param>
public sealed record TerminalFrameLayout(
    IReadOnlyList<TerminalDrawInstruction> Instructions,
    int FirstVisibleOutputLineIndex,
    int VisibleOutputLineCount,
    int MaxVisibleOutputLines,
    int ScrollbackOffsetLines,
    int MaxScrollbackOffsetLines);
