using Avalonia;
using ArbSh.Terminal.Models;

namespace ArbSh.Terminal.Rendering;

/// <summary>
/// محرك تخطيط إطار الرسم لسطح الطرفية.
/// Layout engine that creates draw instructions for a terminal frame.
/// </summary>
public sealed class TerminalLayoutEngine
{
    /// <summary>
    /// يبني تعليمات الرسم فقط (توافق رجعي للاختبارات/الاستخدامات القديمة).
    /// Builds draw instructions only (backward compatibility for older call sites).
    /// </summary>
    /// <param name="logicalLines">أسطر الطرفية بالترتيب المنطقي.</param>
    /// <param name="promptLogical">نص الموجه المنطقي.</param>
    /// <param name="inputLogical">مخزن الإدخال المنطقي.</param>
    /// <param name="surfaceSize">حجم سطح الرسم.</param>
    /// <param name="config">إعدادات الرسم.</param>
    /// <param name="pipeline">خط معالجة النص.</param>
    /// <returns>تعليمات الرسم النهائية.</returns>
    public IReadOnlyList<TerminalDrawInstruction> BuildFrame(
        IReadOnlyList<TerminalLine> logicalLines,
        string promptLogical,
        string inputLogical,
        Size surfaceSize,
        TerminalRenderConfig config,
        TerminalTextPipeline pipeline)
    {
        return BuildFrameLayout(
            logicalLines,
            promptLogical,
            inputLogical,
            surfaceSize,
            config,
            pipeline,
            scrollbackOffsetLines: 0).Instructions;
    }

    /// <summary>
    /// يبني إطارًا كاملاً مع بيانات نافذة العرض والـ scrollback.
    /// Builds a full frame with viewport and scrollback metadata.
    /// </summary>
    /// <param name="logicalLines">أسطر الطرفية بالترتيب المنطقي.</param>
    /// <param name="promptLogical">نص الموجه المنطقي.</param>
    /// <param name="inputLogical">مخزن الإدخال المنطقي.</param>
    /// <param name="surfaceSize">حجم سطح الرسم.</param>
    /// <param name="config">إعدادات الرسم.</param>
    /// <param name="pipeline">خط معالجة النص.</param>
    /// <param name="scrollbackOffsetLines">عدد الأسطر المخفية من ذيل المخرجات.</param>
    /// <returns>ناتج تخطيط الإطار.</returns>
    public TerminalFrameLayout BuildFrameLayout(
        IReadOnlyList<TerminalLine> logicalLines,
        string promptLogical,
        string inputLogical,
        Size surfaceSize,
        TerminalRenderConfig config,
        TerminalTextPipeline pipeline,
        int scrollbackOffsetLines)
    {
        ArgumentNullException.ThrowIfNull(logicalLines);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(pipeline);

        var instructions = new List<TerminalDrawInstruction>();

        double lineHeight = config.LineHeight;
        double top = config.Padding.Top;
        double bottom = config.Padding.Bottom;

        double availableOutputHeight = Math.Max(0, surfaceSize.Height - top - bottom - lineHeight);
        int maxVisibleOutputLines = Math.Max(1, (int)Math.Floor(availableOutputHeight / lineHeight));
        int maxScrollbackOffsetLines = Math.Max(0, logicalLines.Count - maxVisibleOutputLines);
        int clampedOffset = Math.Clamp(scrollbackOffsetLines, 0, maxScrollbackOffsetLines);
        int start = Math.Max(0, logicalLines.Count - maxVisibleOutputLines - clampedOffset);
        int end = Math.Min(logicalLines.Count, start + maxVisibleOutputLines);

        double y = top;
        for (int i = start; i < end; i++)
        {
            TerminalLine line = logicalLines[i];
            VisualTextRun run = pipeline.BuildVisualRun(line.Text, line.Kind, config);
            double x = ResolveX(surfaceSize.Width, run.MeasuredWidth, config, alignRight: run.HasArabic);

            instructions.Add(new TerminalDrawInstruction(
                new Point(x, y),
                run,
                config.ResolveBrush(line.Kind),
                IsPromptLine: false,
                LogicalLineIndex: i));

            y += lineHeight;
        }

        VisualTextRun promptRun = pipeline.BuildPromptRun(promptLogical, inputLogical, config);
        double promptY = Math.Max(top, surfaceSize.Height - bottom - lineHeight);
        double promptX = ResolveX(surfaceSize.Width, promptRun.MeasuredWidth, config, alignRight: true);

        instructions.Add(new TerminalDrawInstruction(
            new Point(promptX, promptY),
            promptRun,
            config.PromptBrush,
            IsPromptLine: true,
            LogicalLineIndex: -1));

        return new TerminalFrameLayout(
            instructions,
            FirstVisibleOutputLineIndex: start,
            VisibleOutputLineCount: end - start,
            MaxVisibleOutputLines: maxVisibleOutputLines,
            ScrollbackOffsetLines: clampedOffset,
            MaxScrollbackOffsetLines: maxScrollbackOffsetLines);
    }

    private static double ResolveX(double width, double textWidth, TerminalRenderConfig config, bool alignRight)
    {
        double left = config.Padding.Left;
        if (!alignRight)
        {
            return left;
        }

        double right = config.Padding.Right;
        return Math.Max(left, width - right - textWidth);
    }
}
