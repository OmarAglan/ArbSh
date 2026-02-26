using ArbSh.Terminal.Rendering;

namespace ArbSh.Test;

public sealed class AnsiSgrParserTests
{
    private readonly AnsiSgrParser _parser = new();

    [Fact]
    public void Parse_StripsBasicForegroundCodes_AndCreatesStyleSpans()
    {
        string source = "\u001b[31mred\u001b[0m plain";

        ParsedTerminalText parsed = _parser.Parse(source);

        Assert.Equal("red plain", parsed.PlainText);
        Assert.NotEmpty(parsed.StyleSpans);

        AnsiStyleSpan first = parsed.StyleSpans[0];
        Assert.Equal(0, first.Start);
        Assert.Equal(3, first.Length);
        Assert.Equal(AnsiColorMode.Indexed16, first.Style.Foreground.Mode);
        Assert.Equal(1, first.Style.Foreground.Index);
    }

    [Fact]
    public void Parse_HandlesIndexed256AndTrueColor()
    {
        string source = "\u001b[38;5;196mX\u001b[48;2;1;2;3mY";

        ParsedTerminalText parsed = _parser.Parse(source);

        Assert.Equal("XY", parsed.PlainText);
        Assert.True(parsed.StyleSpans.Count >= 2);

        AnsiStyleSpan xSpan = parsed.StyleSpans[0];
        Assert.Equal(AnsiColorMode.Indexed256, xSpan.Style.Foreground.Mode);
        Assert.Equal(196, xSpan.Style.Foreground.Index);

        AnsiStyleSpan ySpan = parsed.StyleSpans[1];
        Assert.Equal(AnsiColorMode.TrueColor, ySpan.Style.Background.Mode);
        Assert.Equal((byte)1, ySpan.Style.Background.Red);
        Assert.Equal((byte)2, ySpan.Style.Background.Green);
        Assert.Equal((byte)3, ySpan.Style.Background.Blue);
    }
}
