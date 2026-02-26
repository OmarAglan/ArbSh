namespace ArbSh.Terminal.Rendering;

/// <summary>
/// يحدد طريقة تمثيل اللون في تسلسلات ANSI.
/// Represents the color addressing mode for ANSI styles.
/// </summary>
public enum AnsiColorMode
{
    Default = 0,
    Indexed16 = 1,
    Indexed256 = 2,
    TrueColor = 3
}

/// <summary>
/// مواصفة لون ANSI (افتراضي/مفهرس/TrueColor).
/// ANSI color specification (default/indexed/truecolor).
/// </summary>
public readonly record struct AnsiColorSpec(
    AnsiColorMode Mode,
    byte Index,
    byte Red,
    byte Green,
    byte Blue)
{
    /// <summary>
    /// اللون الافتراضي الخاص بالطرفية.
    /// Terminal default color.
    /// </summary>
    public static AnsiColorSpec Default => new(AnsiColorMode.Default, 0, 0, 0, 0);

    /// <summary>
    /// ينشئ لون ANSI مفهرسًا ضمن جدول 16 لونًا.
    /// Creates a 16-color ANSI indexed color.
    /// </summary>
    /// <param name="index">الفهرس [0..15].</param>
    /// <returns>مواصفة اللون.</returns>
    public static AnsiColorSpec FromIndexed16(int index)
    {
        int clamped = Math.Clamp(index, 0, 15);
        return new AnsiColorSpec(AnsiColorMode.Indexed16, (byte)clamped, 0, 0, 0);
    }

    /// <summary>
    /// ينشئ لون ANSI مفهرسًا ضمن جدول 256 لونًا.
    /// Creates a 256-color ANSI indexed color.
    /// </summary>
    /// <param name="index">الفهرس [0..255].</param>
    /// <returns>مواصفة اللون.</returns>
    public static AnsiColorSpec FromIndexed256(int index)
    {
        int clamped = Math.Clamp(index, 0, 255);
        return new AnsiColorSpec(AnsiColorMode.Indexed256, (byte)clamped, 0, 0, 0);
    }

    /// <summary>
    /// ينشئ لون TrueColor (RGB).
    /// Creates a truecolor RGB specification.
    /// </summary>
    /// <param name="red">قيمة الأحمر [0..255].</param>
    /// <param name="green">قيمة الأخضر [0..255].</param>
    /// <param name="blue">قيمة الأزرق [0..255].</param>
    /// <returns>مواصفة اللون.</returns>
    public static AnsiColorSpec FromTrueColor(int red, int green, int blue)
    {
        byte r = (byte)Math.Clamp(red, 0, 255);
        byte g = (byte)Math.Clamp(green, 0, 255);
        byte b = (byte)Math.Clamp(blue, 0, 255);
        return new AnsiColorSpec(AnsiColorMode.TrueColor, 0, r, g, b);
    }
}
