using Avalonia.Media;

namespace ArbSh.Terminal.Rendering;

/// <summary>
/// سمات ألوان واجهة طرفية ArbSh.
/// ArbSh terminal theme colors.
/// </summary>
public sealed class TerminalTheme
{
    /// <summary>
    /// لون خلفية الطرفية الأساسي.
    /// Terminal background color.
    /// </summary>
    public Color Background { get; init; } = Color.Parse("#10141F");

    /// <summary>
    /// اللون الأساسي للنص العام.
    /// Default foreground color.
    /// </summary>
    public Color Foreground { get; init; } = Color.Parse("#D8DEE9");

    /// <summary>
    /// لون نص الموجه.
    /// Prompt foreground color.
    /// </summary>
    public Color PromptForeground { get; init; } = Color.Parse("#F5F5F5");

    /// <summary>
    /// لون سطر النظام.
    /// System line color.
    /// </summary>
    public Color SystemForeground { get; init; } = Color.Parse("#B9C8E6");

    /// <summary>
    /// لون سطر الإدخال.
    /// Input line color.
    /// </summary>
    public Color InputForeground { get; init; } = Color.Parse("#5EC8FF");

    /// <summary>
    /// لون التحذير.
    /// Warning line color.
    /// </summary>
    public Color WarningForeground { get; init; } = Color.Parse("#F5C15C");

    /// <summary>
    /// لون الأخطاء.
    /// Error line color.
    /// </summary>
    public Color ErrorForeground { get; init; } = Color.Parse("#F07178");

    /// <summary>
    /// لون الرسائل التشخيصية.
    /// Debug line color.
    /// </summary>
    public Color DebugForeground { get; init; } = Color.Parse("#8B93A6");

    /// <summary>
    /// لون تحديد الإدخال.
    /// Input selection background color.
    /// </summary>
    public Color InputSelectionBackground { get; init; } = Color.FromArgb(96, 80, 150, 255);

    /// <summary>
    /// لون تحديد المخرجات.
    /// Output selection background color.
    /// </summary>
    public Color OutputSelectionBackground { get; init; } = Color.FromArgb(72, 75, 130, 220);

    /// <summary>
    /// ينشئ سمة ArbSh الافتراضية ذات الخلفية البحرية الداكنة.
    /// Creates the default ArbSh navy theme.
    /// </summary>
    /// <returns>سمة كاملة.</returns>
    public static TerminalTheme CreateArbShNavy()
    {
        return new TerminalTheme();
    }
}
