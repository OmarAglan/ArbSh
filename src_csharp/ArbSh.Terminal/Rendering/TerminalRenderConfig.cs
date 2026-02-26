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
    /// عائلة الخط الأساسية مع سلسلة بدائل وقت التشغيل.
    /// Primary font family with runtime fallback chain.
    /// </summary>
    public FontFamily FontFamily { get; init; } = new("Cascadia Mono, Cascadia Code, Noto Sans Arabic UI, Segoe UI, Consolas, Courier New");

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
    public IBrush PromptBrush { get; init; } = Brushes.Gainsboro;

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
    public FormattedText CreateFormattedText(string text, IBrush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
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
            TerminalLineKind.System => Brushes.LightSteelBlue,
            TerminalLineKind.Input => Brushes.DeepSkyBlue,
            TerminalLineKind.Warning => Brushes.Goldenrod,
            TerminalLineKind.Error => Brushes.IndianRed,
            TerminalLineKind.Debug => Brushes.Gray,
            _ => Brushes.Gainsboro
        };
    }
}
