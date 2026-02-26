using Avalonia;
using ArbSh.Terminal.Models;
using ArbSh.Terminal.Rendering;

namespace ArbSh.Test;

public sealed class TerminalLayoutEngineTests
{
    [Fact]
    public void BuildFrame_AppliesDirectionalAlignmentForOutputLines()
    {
        var config = new TerminalRenderConfig
        {
            Padding = new Thickness(10),
            LineHeight = 20
        };

        var lines = new List<TerminalLine>
        {
            new("abc", TerminalLineKind.Output, DateTimeOffset.UtcNow),
            new("مرحبا", TerminalLineKind.Output, DateTimeOffset.UtcNow)
        };

        var engine = new TerminalLayoutEngine();
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());

        IReadOnlyList<TerminalDrawInstruction> frame = engine.BuildFrame(
            lines,
            "أربش> ",
            string.Empty,
            new Size(200, 120),
            config,
            pipeline);

        TerminalDrawInstruction ltr = Assert.Single(frame.Where(x => x.Run.LogicalText == "abc"));
        TerminalDrawInstruction rtl = Assert.Single(frame.Where(x => x.Run.LogicalText == "مرحبا"));

        Assert.Equal(config.Padding.Left, ltr.Position.X);
        Assert.True(rtl.Position.X > config.Padding.Left);
    }

    [Fact]
    public void BuildFrame_TrimsToVisibleOutputWindow()
    {
        var config = new TerminalRenderConfig
        {
            Padding = new Thickness(10),
            LineHeight = 20
        };

        var lines = Enumerable.Range(0, 10)
            .Select(i => new TerminalLine($"line-{i}", TerminalLineKind.Output, DateTimeOffset.UtcNow))
            .ToList();

        var engine = new TerminalLayoutEngine();
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());

        IReadOnlyList<TerminalDrawInstruction> frame = engine.BuildFrame(
            lines,
            "أربش> ",
            string.Empty,
            new Size(220, 130),
            config,
            pipeline);

        List<TerminalDrawInstruction> outputs = [.. frame.Where(x => !x.IsPromptLine)];

        Assert.Equal(4, outputs.Count);
        Assert.Equal("line-6", outputs[0].Run.LogicalText);
        Assert.Equal("line-9", outputs[^1].Run.LogicalText);
    }

    [Fact]
    public void BuildFrame_AddsPromptInstructionNearBottom()
    {
        var config = new TerminalRenderConfig
        {
            Padding = new Thickness(10),
            LineHeight = 20
        };

        var engine = new TerminalLayoutEngine();
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());

        IReadOnlyList<TerminalDrawInstruction> frame = engine.BuildFrame(
            [],
            "أربش> ",
            "Get-Command",
            new Size(240, 140),
            config,
            pipeline);

        TerminalDrawInstruction prompt = Assert.Single(frame.Where(x => x.IsPromptLine));
        double expectedPromptY = Math.Max(config.Padding.Top, 140 - config.Padding.Bottom - config.LineHeight);
        Assert.Equal(expectedPromptY, prompt.Position.Y);
    }

    [Fact]
    public void BuildFrameLayout_AppliesScrollbackOffsetWindow()
    {
        var config = new TerminalRenderConfig
        {
            Padding = new Thickness(10),
            LineHeight = 20
        };

        var lines = Enumerable.Range(0, 8)
            .Select(i => new TerminalLine($"line-{i}", TerminalLineKind.Output, DateTimeOffset.UtcNow))
            .ToList();

        var engine = new TerminalLayoutEngine();
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());

        TerminalFrameLayout frame = engine.BuildFrameLayout(
            lines,
            "أربش> ",
            string.Empty,
            new Size(220, 130),
            config,
            pipeline,
            scrollbackOffsetLines: 2);

        List<TerminalDrawInstruction> outputs = [.. frame.Instructions.Where(x => !x.IsPromptLine)];

        Assert.Equal(4, outputs.Count);
        Assert.Equal("line-2", outputs[0].Run.LogicalText);
        Assert.Equal("line-5", outputs[^1].Run.LogicalText);
        Assert.Equal(2, frame.FirstVisibleOutputLineIndex);
        Assert.Equal(4, frame.VisibleOutputLineCount);
        Assert.Equal(2, frame.ScrollbackOffsetLines);
    }

    [Fact]
    public void BuildFrameLayout_ClampsScrollbackOffsetToMaximum()
    {
        var config = new TerminalRenderConfig
        {
            Padding = new Thickness(10),
            LineHeight = 20
        };

        var lines = Enumerable.Range(0, 6)
            .Select(i => new TerminalLine($"line-{i}", TerminalLineKind.Output, DateTimeOffset.UtcNow))
            .ToList();

        var engine = new TerminalLayoutEngine();
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());

        TerminalFrameLayout frame = engine.BuildFrameLayout(
            lines,
            "أربش> ",
            string.Empty,
            new Size(220, 130),
            config,
            pipeline,
            scrollbackOffsetLines: 999);

        Assert.Equal(2, frame.MaxScrollbackOffsetLines);
        Assert.Equal(frame.MaxScrollbackOffsetLines, frame.ScrollbackOffsetLines);
        Assert.Equal(0, frame.FirstVisibleOutputLineIndex);
    }

    private sealed class FakeTextMeasurer : ITextMeasurer
    {
        public double MeasureWidth(string visualText, TerminalRenderConfig config)
        {
            return visualText.Length;
        }
    }
}
