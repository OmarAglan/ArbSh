using Avalonia;
using ArbSh.Terminal.Models;
using ArbSh.Terminal.Rendering;

namespace ArbSh.Test;

public sealed class LogicalVisualSeparationTests
{
    [Fact]
    public void BuildFrame_DoesNotMutateLogicalLineStorage()
    {
        const string logicalLine = "مساعدة";
        var lines = new List<TerminalLine>
        {
            new(logicalLine, TerminalLineKind.Output, DateTimeOffset.UtcNow)
        };

        var config = new TerminalRenderConfig
        {
            Padding = new Thickness(10),
            LineHeight = 20
        };

        var engine = new TerminalLayoutEngine();
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());

        _ = engine.BuildFrame(
            lines,
            "أربش< ",
            string.Empty,
            new Size(220, 120),
            config,
            pipeline);

        Assert.Equal(logicalLine, lines[0].Text);
    }

    [Fact]
    public void BuildVisualRun_ReturnsOriginalLogicalTextAlongsideVisual()
    {
        const string logical = "أربش< مساعدة";
        var config = new TerminalRenderConfig();
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());

        VisualTextRun run = pipeline.BuildVisualRun(logical, TerminalLineKind.Input, config);

        Assert.Equal(logical, run.LogicalText);
        Assert.NotEmpty(run.VisualText);
    }

    private sealed class FakeTextMeasurer : ITextMeasurer
    {
        public double MeasureWidth(string visualText, TerminalRenderConfig config)
        {
            return visualText.Length;
        }
    }
}
