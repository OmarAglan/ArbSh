using Avalonia.Media;

namespace ArbSh.Terminal.Rendering;

/// <summary>
/// لوحة ألوان ANSI (16 لونًا أساسيًا + تحويل 256 لونًا).
/// ANSI palette (base 16 colors + 256-color mapping).
/// </summary>
public sealed class AnsiPalette
{
    private readonly Color[] _base16;

    /// <summary>
    /// ينشئ لوحة ANSI من 16 لونًا أساسيًا.
    /// Creates an ANSI palette from 16 base colors.
    /// </summary>
    /// <param name="base16">ألوان ANSI الأساسية.</param>
    public AnsiPalette(IReadOnlyList<Color> base16)
    {
        ArgumentNullException.ThrowIfNull(base16);
        if (base16.Count != 16)
        {
            throw new ArgumentException("ANSI base palette must contain exactly 16 colors.", nameof(base16));
        }

        _base16 = [.. base16];
    }

    /// <summary>
    /// يحول فهرس ANSI (16/256) إلى لون فعلي.
    /// Resolves ANSI indexed color (16/256) to concrete color.
    /// </summary>
    /// <param name="index">فهرس اللون.</param>
    /// <returns>اللون المقابل.</returns>
    public Color ResolveIndexed(int index)
    {
        int clamped = Math.Clamp(index, 0, 255);
        if (clamped < 16)
        {
            return _base16[clamped];
        }

        if (clamped <= 231)
        {
            int cube = clamped - 16;
            int r = cube / 36;
            int g = (cube / 6) % 6;
            int b = cube % 6;

            return Color.FromRgb(CubeToByte(r), CubeToByte(g), CubeToByte(b));
        }

        int gray = clamped - 232;
        byte level = (byte)(8 + (gray * 10));
        return Color.FromRgb(level, level, level);
    }

    /// <summary>
    /// لوحة ArbSh الافتراضية للوضع الداكن.
    /// Default ArbSh dark ANSI palette.
    /// </summary>
    /// <returns>لوحة ANSI مهيأة.</returns>
    public static AnsiPalette CreateArbShNavy()
    {
        Color[] colors =
        [
            Color.Parse("#111827"), // black
            Color.Parse("#EF4444"), // red
            Color.Parse("#22C55E"), // green
            Color.Parse("#F59E0B"), // yellow
            Color.Parse("#3B82F6"), // blue
            Color.Parse("#A855F7"), // magenta
            Color.Parse("#06B6D4"), // cyan
            Color.Parse("#E5E7EB"), // white
            Color.Parse("#374151"), // bright black
            Color.Parse("#F87171"), // bright red
            Color.Parse("#4ADE80"), // bright green
            Color.Parse("#FBBF24"), // bright yellow
            Color.Parse("#60A5FA"), // bright blue
            Color.Parse("#C084FC"), // bright magenta
            Color.Parse("#22D3EE"), // bright cyan
            Color.Parse("#F9FAFB")  // bright white
        ];

        return new AnsiPalette(colors);
    }

    private static byte CubeToByte(int value)
    {
        return value switch
        {
            <= 0 => 0,
            1 => 95,
            2 => 135,
            3 => 175,
            4 => 215,
            _ => 255
        };
    }
}
