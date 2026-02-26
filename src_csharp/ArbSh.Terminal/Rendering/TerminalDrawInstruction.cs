using Avalonia;
using Avalonia.Media;

namespace ArbSh.Terminal.Rendering;

/// <summary>
/// تعليمات رسم سطر واحد على سطح الطرفية.
/// A single draw instruction for the terminal surface.
/// </summary>
/// <param name="Position">موضع الرسم على السطح.</param>
/// <param name="Run">بيانات النص المرئي.</param>
/// <param name="Brush">فرشاة الرسم.</param>
/// <param name="IsPromptLine">هل هذا السطر هو سطر الموجه.</param>
/// <param name="LogicalLineIndex">فهرس السطر المنطقي داخل مخزن المخرجات (-1 للموجه).</param>
public sealed record TerminalDrawInstruction(
    Point Position,
    VisualTextRun Run,
    IBrush Brush,
    bool IsPromptLine = false,
    int LogicalLineIndex = -1);
