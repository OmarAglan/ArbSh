using ArbSh.Core.I18n;
using ArbSh.Terminal.Models;

namespace ArbSh.Terminal.Rendering;

/// <summary>
/// خط معالجة النص بين التمثيل المنطقي الداخلي والتمثيل المرئي للرسم.
/// Text pipeline that transforms logical terminal text to visual draw-ready text.
/// </summary>
public sealed class TerminalTextPipeline
{
    private readonly ITextMeasurer _measurer;

    /// <summary>
    /// ينشئ معالج النص مع مقياس العرض المطلوب.
    /// Creates a text pipeline with a pluggable text measurer.
    /// </summary>
    /// <param name="measurer">مقياس النص (اختياري).</param>
    public TerminalTextPipeline(ITextMeasurer? measurer = null)
    {
        _measurer = measurer ?? new AvaloniaTextMeasurer();
    }

    /// <summary>
    /// يبني سطرًا مرئيًا من نص منطقي لسطر طرفية عادي.
    /// Builds a visual run from a logical terminal line.
    /// </summary>
    /// <param name="logicalText">النص المنطقي.</param>
    /// <param name="kind">نوع السطر.</param>
    /// <param name="config">إعدادات الرسم.</param>
    /// <returns>بيانات السطر المرئي.</returns>
    public VisualTextRun BuildVisualRun(string logicalText, TerminalLineKind kind, TerminalRenderConfig config)
    {
        string safeLogical = logicalText ?? string.Empty;
        bool hasArabic = BiDiTextProcessor.ContainsArabicText(safeLogical);
        string visual = ToVisual(safeLogical, hasArabic);
        double width = _measurer.MeasureWidth(visual, config);

        return new VisualTextRun(safeLogical, visual, hasArabic, width, kind);
    }

    /// <summary>
    /// يبني سطرًا مرئيًا لسطر الموجه والمدخلات الحالية.
    /// Builds a visual run for prompt + current input buffer.
    /// </summary>
    /// <param name="promptLogical">نص الموجه المنطقي.</param>
    /// <param name="inputLogical">نص الإدخال المنطقي.</param>
    /// <param name="config">إعدادات الرسم.</param>
    /// <returns>بيانات السطر المرئي للموجه.</returns>
    public VisualTextRun BuildPromptRun(string promptLogical, string inputLogical, TerminalRenderConfig config)
    {
        string logical = string.Concat(promptLogical ?? string.Empty, inputLogical ?? string.Empty);
        return BuildVisualRun(logical, TerminalLineKind.Input, config);
    }

    private static string ToVisual(string logicalText, bool hasArabic)
    {
        if (!hasArabic || string.IsNullOrEmpty(logicalText))
        {
            return logicalText;
        }

        try
        {
            // Keep logical text intact and perform visual conversion only at render boundary.
            var runs = BidiAlgorithm.ProcessRuns(logicalText, -1);
            return BidiAlgorithm.ReorderRunsForDisplay(logicalText, runs);
        }
        catch
        {
            // Rendering failure should not corrupt logical shell state.
            return logicalText;
        }
    }
}
