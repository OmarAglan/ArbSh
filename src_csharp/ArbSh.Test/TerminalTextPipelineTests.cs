using ArbSh.Terminal.Models;
using ArbSh.Terminal.Rendering;

namespace ArbSh.Test;

public sealed class TerminalTextPipelineTests
{
    private static readonly TerminalRenderConfig RenderConfig = new();

    [Fact]
    public void BuildVisualRun_LatinText_PreservesVisualOrder()
    {
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());
        const string logical = "Get-Command";

        VisualTextRun run = pipeline.BuildVisualRun(logical, TerminalLineKind.Output, RenderConfig);

        Assert.Equal(logical, run.LogicalText);
        Assert.Equal(logical, run.VisualText);
        Assert.False(run.HasArabic);
        Assert.Equal(logical.Length, run.MeasuredWidth);
    }

    [Fact]
    public void BuildVisualRun_ArabicText_KeepsLogicalStateIntact()
    {
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());
        const string logical = "احصل-مساعدة";

        VisualTextRun run = pipeline.BuildVisualRun(logical, TerminalLineKind.Output, RenderConfig);

        Assert.Equal(logical, run.LogicalText);
        Assert.True(run.HasArabic);
        Assert.NotEmpty(run.VisualText);
        Assert.Equal(run.VisualText.Length, run.MeasuredWidth);
    }

    [Fact]
    public void BuildPromptRun_ComposesPromptAndInputAsLogicalText()
    {
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());

        VisualTextRun run = pipeline.BuildPromptRun("أربش> ", "Get-Command", RenderConfig);

        Assert.Equal("أربش> Get-Command", run.LogicalText);
        Assert.True(run.HasArabic);
    }

    [Fact]
    public void BuildVisualRun_StripsAnsiEscapes_AndKeepsLogicalSource()
    {
        var pipeline = new TerminalTextPipeline(new FakeTextMeasurer());
        const string logical = "\u001b[31mERROR\u001b[0m";

        VisualTextRun run = pipeline.BuildVisualRun(logical, TerminalLineKind.Output, RenderConfig);

        Assert.Equal(logical, run.LogicalText);
        Assert.Equal("ERROR", run.VisualText);
        Assert.True(run.StyleSpans.Count > 0);
        Assert.Equal("ERROR".Length, run.MeasuredWidth);
    }

    private sealed class FakeTextMeasurer : ITextMeasurer
    {
        public double MeasureWidth(string visualText, TerminalRenderConfig config)
        {
            return visualText.Length;
        }
    }
}
