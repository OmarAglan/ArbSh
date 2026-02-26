using ArbSh.Terminal.Models;

namespace ArbSh.Terminal.Rendering;

/// <summary>
/// يمثل نتيجة تحويل سطر نصي من الترتيب المنطقي إلى الترتيب المرئي.
/// Represents a logical-to-visual transformed text run.
/// </summary>
/// <param name="LogicalText">النص المنطقي الأصلي.</param>
/// <param name="VisualText">النص المرئي الجاهز للرسم.</param>
/// <param name="HasArabic">هل يحتوي النص المنطقي على العربية.</param>
/// <param name="MeasuredWidth">العرض المقاس للنص المرئي.</param>
/// <param name="Kind">نوع السطر المنطقي.</param>
public sealed record VisualTextRun(
    string LogicalText,
    string VisualText,
    bool HasArabic,
    double MeasuredWidth,
    TerminalLineKind Kind);
