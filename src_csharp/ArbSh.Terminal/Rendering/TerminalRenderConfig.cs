using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using ArbSh.Terminal.Models;

namespace ArbSh.Terminal.Rendering;

/// <summary>
/// إعدادات العرض الخاصة بسطح الطرفية (الخطوط، الألوان، الهوامش).
/// Terminal rendering configuration (fonts, colors, and spacing).
/// </summary>
public sealed class TerminalRenderConfig
{
    /// <summary>
    /// سمة ArbSh المستخدمة في ألوان الواجهة الافتراضية.
    /// ArbSh UI theme used for default terminal colors.
    /// </summary>
    public TerminalTheme Theme { get; init; } = TerminalTheme.CreateArbShNavy();

    /// <summary>
    /// لوحة ألوان ANSI المستخدمة لحل الأكواد المفهرسة.
    /// ANSI palette used to resolve indexed color codes.
    /// </summary>
    public AnsiPalette Palette { get; init; } = AnsiPalette.CreateArbShNavy();

    /// <summary>
    /// عائلة الخط الأساسية مع سلسلة بدائل وقت التشغيل.
    /// Primary font family with runtime fallback chain.
    /// </summary>
    public FontFamily FontFamily { get; init; } = new(
        "avares://ArbSh.Terminal/Assets/Fonts/CascadiaMono.ttf#Cascadia Mono, " +
        "avares://ArbSh.Terminal/Assets/Fonts/arabtype.ttf#Arabic Typesetting, " +
        "Cascadia Mono, Cascadia Code, Arabic Typesetting, Noto Sans Arabic UI, Segoe UI, Consolas, Courier New");

    /// <summary>
    /// مقاس الخط المستخدم في الرسم.
    /// Font size used for drawing.
    /// </summary>
    public double FontSize { get; init; } = 15;

    /// <summary>
    /// ارتفاع السطر في مساحة الرسم.
    /// Line height for terminal rows.
    /// </summary>
    public double LineHeight { get; init; } = 22;

    /// <summary>
    /// الهوامش الداخلية لسطح الرسم.
    /// Surface padding.
    /// </summary>
    public Thickness Padding { get; init; } = new(14, 10, 14, 10);

    /// <summary>
    /// لون الخلفية.
    /// Surface background brush.
    /// </summary>
    public IBrush BackgroundBrush { get; init; } = new SolidColorBrush(Color.Parse("#10141F"));

    /// <summary>
    /// لون سطر الموجّه (Prompt).
    /// Prompt line brush.
    /// </summary>
    public IBrush PromptBrush { get; init; } = new SolidColorBrush(Color.Parse("#F5F5F5"));

    /// <summary>
    /// لون خلفية التحديد داخل سطر الإدخال.
    /// Selection background brush for input line.
    /// </summary>
    public IBrush SelectionBrush { get; init; } = new SolidColorBrush(Color.FromArgb(96, 80, 150, 255));

    /// <summary>
    /// لون خلفية تحديد أسطر المخرجات.
    /// Selection background brush for output lines.
    /// </summary>
    public IBrush OutputSelectionBrush { get; init; } = new SolidColorBrush(Color.FromArgb(72, 75, 130, 220));

    /// <summary>
    /// عدد الأسطر التي يتم تمريرها لكل خطوة عجلة.
    /// Number of lines to scroll per wheel step.
    /// </summary>
    public int ScrollLinesPerWheelStep { get; init; } = 3;

    /// <summary>
    /// نوع الخط المستخدم في الرسم.
    /// Typeface used by Avalonia text layout.
    /// </summary>
    public Typeface Typeface => new(FontFamily, FontStyle.Normal, FontWeight.Normal);

    /// <summary>
    /// ينشئ كائن نص جاهز للرسم والقياس.
    /// Creates a formatted text object for drawing and measurement.
    /// </summary>
    /// <param name="text">النص المراد رسمه.</param>
    /// <param name="brush">فرشاة الرسم.</param>
    /// <returns>كائن النص المنسق.</returns>
    public FormattedText CreateFormattedText(string text, IBrush brush, FlowDirection flowDirection = FlowDirection.LeftToRight)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            flowDirection,
            Typeface,
            FontSize,
            brush);
    }

    /// <summary>
    /// ينشئ تخطيط نص يدعم اختبارات موضع المؤشر بدقة مع BiDi.
    /// Creates a text layout that supports accurate BiDi-aware caret hit testing.
    /// </summary>
    /// <param name="text">النص المنطقي المراد رسمه.</param>
    /// <param name="brush">فرشاة الرسم.</param>
    /// <param name="flowDirection">اتجاه الفقرة الأساسي.</param>
    /// <returns>تخطيط النص.</returns>
    public TextLayout CreateTextLayout(string text, IBrush brush, FlowDirection flowDirection)
    {
        return new TextLayout(
            text: text,
            typeface: Typeface,
            fontSize: FontSize,
            foreground: brush,
            textAlignment: TextAlignment.Left,
            textWrapping: TextWrapping.NoWrap,
            textTrimming: TextTrimming.None,
            textDecorations: null,
            flowDirection: flowDirection,
            maxWidth: double.PositiveInfinity,
            maxHeight: double.PositiveInfinity,
            lineHeight: LineHeight,
            letterSpacing: 0,
            maxLines: 1,
            textStyleOverrides: null);
    }

    /// <summary>
    /// يحدد لون السطر حسب نوعه المنطقي.
    /// Resolves line brush by line kind.
    /// </summary>
    /// <param name="kind">نوع السطر.</param>
    /// <returns>الفرشاة المناسبة.</returns>
    public IBrush ResolveBrush(TerminalLineKind kind)
    {
        return kind switch
        {
            TerminalLineKind.System => new SolidColorBrush(Theme.SystemForeground),
            TerminalLineKind.Input => new SolidColorBrush(Theme.InputForeground),
            TerminalLineKind.Warning => new SolidColorBrush(Theme.WarningForeground),
            TerminalLineKind.Error => new SolidColorBrush(Theme.ErrorForeground),
            TerminalLineKind.Debug => new SolidColorBrush(Theme.DebugForeground),
            _ => new SolidColorBrush(Theme.Foreground)
        };
    }

    /// <summary>
    /// يحل لون النص النهائي لنطاق ANSI مع قواعد العكس/الإخفاء/التعتيم.
    /// Resolves final foreground brush for an ANSI span with inverse/hidden/dim behavior.
    /// </summary>
    /// <param name="kind">نوع السطر.</param>
    /// <param name="style">حالة النمط.</param>
    /// <returns>فرشاة النص النهائية.</returns>
    public IBrush ResolveAnsiForegroundBrush(TerminalLineKind kind, AnsiStyleState style)
    {
        IBrush baseForeground = ResolveBrush(kind);
        IBrush baseBackground = BackgroundBrush;

        IBrush fg = ResolveAnsiColor(style.Foreground, baseForeground);
        IBrush bg = ResolveAnsiColor(style.Background, baseBackground);

        if (style.Inverse)
        {
            (fg, bg) = (bg, fg);
        }

        if (style.Hidden)
        {
            fg = bg;
        }

        if (style.Dim)
        {
            fg = DimBrush(fg);
        }

        return fg;
    }

    /// <summary>
    /// يحل خلفية النطاق وفق ANSI، أو يعيد null إذا لا توجد خلفية مطلوبة.
    /// Resolves ANSI background brush, or null when no explicit background is needed.
    /// </summary>
    /// <param name="kind">نوع السطر.</param>
    /// <param name="style">حالة النمط.</param>
    /// <returns>فرشاة الخلفية أو null.</returns>
    public IBrush? ResolveAnsiBackgroundBrush(TerminalLineKind kind, AnsiStyleState style)
    {
        bool needsBackground = style.Background.Mode != AnsiColorMode.Default || style.Inverse || style.Hidden;
        if (!needsBackground)
        {
            return null;
        }

        IBrush baseForeground = ResolveBrush(kind);
        IBrush baseBackground = BackgroundBrush;

        IBrush fg = ResolveAnsiColor(style.Foreground, baseForeground);
        IBrush bg = ResolveAnsiColor(style.Background, baseBackground);

        if (style.Inverse)
        {
            (fg, bg) = (bg, fg);
        }

        return bg;
    }

    /// <summary>
    /// يحل وزن الخط لنمط ANSI.
    /// Resolves font weight from ANSI style.
    /// </summary>
    /// <param name="style">حالة النمط.</param>
    /// <returns>وزن الخط.</returns>
    public FontWeight ResolveAnsiFontWeight(AnsiStyleState style)
    {
        return style.Bold ? FontWeight.Bold : FontWeight.Normal;
    }

    /// <summary>
    /// يحل نمط الخط (مائل/عادي) لنمط ANSI.
    /// Resolves font style from ANSI style.
    /// </summary>
    /// <param name="style">حالة النمط.</param>
    /// <returns>نمط الخط.</returns>
    public FontStyle ResolveAnsiFontStyle(AnsiStyleState style)
    {
        return style.Italic ? FontStyle.Italic : FontStyle.Normal;
    }

    /// <summary>
    /// يحل زخارف النص (تسطير/شطب) لنمط ANSI.
    /// Resolves text decorations from ANSI style.
    /// </summary>
    /// <param name="style">حالة النمط.</param>
    /// <returns>زخارف النص أو null.</returns>
    public TextDecorationCollection? ResolveAnsiTextDecorations(AnsiStyleState style)
    {
        if (!style.Underline && !style.Strikethrough)
        {
            return null;
        }

        if (style.Underline && style.Strikethrough)
        {
            var all = new TextDecorationCollection();
            all.Add(TextDecorations.Underline[0]);
            all.Add(TextDecorations.Strikethrough[0]);
            return all;
        }

        return style.Underline ? TextDecorations.Underline : TextDecorations.Strikethrough;
    }

    private IBrush ResolveAnsiColor(AnsiColorSpec spec, IBrush fallback)
    {
        return spec.Mode switch
        {
            AnsiColorMode.Default => fallback,
            AnsiColorMode.Indexed16 => new SolidColorBrush(Palette.ResolveIndexed(spec.Index)),
            AnsiColorMode.Indexed256 => new SolidColorBrush(Palette.ResolveIndexed(spec.Index)),
            AnsiColorMode.TrueColor => new SolidColorBrush(Color.FromRgb(spec.Red, spec.Green, spec.Blue)),
            _ => fallback
        };
    }

    private static IBrush DimBrush(IBrush brush)
    {
        if (brush is not ISolidColorBrush solid)
        {
            return brush;
        }

        byte alpha = (byte)Math.Clamp((int)(solid.Color.A * 0.65), 0, 255);
        Color c = solid.Color;
        return new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
    }
}
