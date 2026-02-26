namespace ArbSh.Terminal.Rendering;

/// <summary>
/// حالة نمط ANSI النشطة لسلسلة نصية.
/// Active ANSI style state for a text range.
/// </summary>
public readonly record struct AnsiStyleState(
    AnsiColorSpec Foreground,
    AnsiColorSpec Background,
    bool Bold,
    bool Dim,
    bool Italic,
    bool Underline,
    bool Inverse,
    bool Hidden,
    bool Strikethrough)
{
    /// <summary>
    /// الحالة الافتراضية بدون أي تنسيق.
    /// Default style state with no ANSI formatting.
    /// </summary>
    public static AnsiStyleState Default => new(
        Foreground: AnsiColorSpec.Default,
        Background: AnsiColorSpec.Default,
        Bold: false,
        Dim: false,
        Italic: false,
        Underline: false,
        Inverse: false,
        Hidden: false,
        Strikethrough: false);

    /// <summary>
    /// هل هذه الحالة هي الحالة الافتراضية بالكامل.
    /// Indicates whether this state equals full default style.
    /// </summary>
    public bool IsDefault =>
        Foreground.Mode == AnsiColorMode.Default &&
        Background.Mode == AnsiColorMode.Default &&
        !Bold &&
        !Dim &&
        !Italic &&
        !Underline &&
        !Inverse &&
        !Hidden &&
        !Strikethrough;
}

/// <summary>
/// نطاق نمط ANSI فوق نص منطقي بعد إزالة أكواد الهروب.
/// ANSI style span over plain text after escape stripping.
/// </summary>
/// <param name="Start">بداية النطاق (شاملة).</param>
/// <param name="Length">طول النطاق.</param>
/// <param name="Style">حالة النمط المطبقة على النطاق.</param>
public readonly record struct AnsiStyleSpan(int Start, int Length, AnsiStyleState Style);

/// <summary>
/// ناتج تحليل نص يحتوي ANSI إلى نص مرئي + نطاقات نمط.
/// Parsed ANSI text result (plain text + style spans).
/// </summary>
/// <param name="PlainText">النص بعد إزالة تسلسلات ANSI.</param>
/// <param name="StyleSpans">نطاقات الأنماط على النص الناتج.</param>
public sealed record ParsedTerminalText(
    string PlainText,
    IReadOnlyList<AnsiStyleSpan> StyleSpans);
