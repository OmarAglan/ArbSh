using System.Globalization;
using System.Text;

namespace ArbSh.Terminal.Rendering;

/// <summary>
/// محلل تسلسلات ANSI SGR لتحويل النص إلى نص نظيف + نطاقات تنسيق.
/// ANSI SGR parser that converts text to plain text + style spans.
/// </summary>
public sealed class AnsiSgrParser
{
    private const char Escape = '\u001B';

    /// <summary>
    /// يحلل النص المنطقي ويستخرج تنسيقات ANSI SGR.
    /// Parses logical text and extracts ANSI SGR styling.
    /// </summary>
    /// <param name="logicalText">النص الأصلي المحتمل أن يحتوي ANSI.</param>
    /// <returns>النص النظيف مع نطاقات التنسيق.</returns>
    public ParsedTerminalText Parse(string logicalText)
    {
        string source = logicalText ?? string.Empty;
        if (source.Length == 0)
        {
            return new ParsedTerminalText(string.Empty, []);
        }

        var plain = new StringBuilder(source.Length);
        var spans = new List<AnsiStyleSpan>();
        AnsiStyleState current = AnsiStyleState.Default;
        int spanStart = 0;

        int i = 0;
        while (i < source.Length)
        {
            if (source[i] == Escape && TryParseSgrSequence(source, i, out int consumedChars, out List<int> codes))
            {
                AppendSpanIfAny(spans, spanStart, plain.Length - spanStart, current);
                ApplyCodes(ref current, codes);
                spanStart = plain.Length;
                i += consumedChars;
                continue;
            }

            plain.Append(source[i]);
            i++;
        }

        AppendSpanIfAny(spans, spanStart, plain.Length - spanStart, current);

        string plainText = plain.ToString();
        if (plainText.Length == 0)
        {
            return new ParsedTerminalText(string.Empty, []);
        }

        if (spans.Count == 0)
        {
            spans.Add(new AnsiStyleSpan(0, plainText.Length, AnsiStyleState.Default));
        }

        return new ParsedTerminalText(plainText, spans);
    }

    private static void AppendSpanIfAny(List<AnsiStyleSpan> spans, int start, int length, AnsiStyleState style)
    {
        if (length <= 0)
        {
            return;
        }

        spans.Add(new AnsiStyleSpan(start, length, style));
    }

    private static bool TryParseSgrSequence(string source, int index, out int consumedChars, out List<int> codes)
    {
        consumedChars = 0;
        codes = [];

        if (index + 2 >= source.Length || source[index] != Escape || source[index + 1] != '[')
        {
            return false;
        }

        int j = index + 2;
        var token = new StringBuilder();
        while (j < source.Length)
        {
            char c = source[j];
            if (c == 'm')
            {
                if (token.Length == 0)
                {
                    codes.Add(0);
                }
                else
                {
                    if (!TryParseCodeList(token.ToString(), codes))
                    {
                        return false;
                    }
                }

                consumedChars = j - index + 1;
                return true;
            }

            if (!char.IsDigit(c) && c != ';')
            {
                return false;
            }

            token.Append(c);
            j++;
        }

        return false;
    }

    private static bool TryParseCodeList(string raw, List<int> codes)
    {
        string[] parts = raw.Split(';');
        if (parts.Length == 0)
        {
            codes.Add(0);
            return true;
        }

        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                codes.Add(0);
                continue;
            }

            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return false;
            }

            codes.Add(value);
        }

        return true;
    }

    private static void ApplyCodes(ref AnsiStyleState state, List<int> codes)
    {
        if (codes.Count == 0)
        {
            state = AnsiStyleState.Default;
            return;
        }

        int i = 0;
        while (i < codes.Count)
        {
            int code = codes[i];
            switch (code)
            {
                case 0:
                    state = AnsiStyleState.Default;
                    break;

                case 1:
                    state = state with { Bold = true };
                    break;

                case 2:
                    state = state with { Dim = true };
                    break;

                case 3:
                    state = state with { Italic = true };
                    break;

                case 4:
                    state = state with { Underline = true };
                    break;

                case 7:
                    state = state with { Inverse = true };
                    break;

                case 8:
                    state = state with { Hidden = true };
                    break;

                case 9:
                    state = state with { Strikethrough = true };
                    break;

                case 22:
                    state = state with { Bold = false, Dim = false };
                    break;

                case 23:
                    state = state with { Italic = false };
                    break;

                case 24:
                    state = state with { Underline = false };
                    break;

                case 27:
                    state = state with { Inverse = false };
                    break;

                case 28:
                    state = state with { Hidden = false };
                    break;

                case 29:
                    state = state with { Strikethrough = false };
                    break;

                case 39:
                    state = state with { Foreground = AnsiColorSpec.Default };
                    break;

                case 49:
                    state = state with { Background = AnsiColorSpec.Default };
                    break;

                default:
                    if (code is >= 30 and <= 37)
                    {
                        state = state with { Foreground = AnsiColorSpec.FromIndexed16(code - 30) };
                    }
                    else if (code is >= 90 and <= 97)
                    {
                        state = state with { Foreground = AnsiColorSpec.FromIndexed16(code - 90 + 8) };
                    }
                    else if (code is >= 40 and <= 47)
                    {
                        state = state with { Background = AnsiColorSpec.FromIndexed16(code - 40) };
                    }
                    else if (code is >= 100 and <= 107)
                    {
                        state = state with { Background = AnsiColorSpec.FromIndexed16(code - 100 + 8) };
                    }
                    else if (code == 38 || code == 48)
                    {
                        if (TryParseExtendedColor(codes, i, out int consumed, out AnsiColorSpec parsed))
                        {
                            if (code == 38)
                            {
                                state = state with { Foreground = parsed };
                            }
                            else
                            {
                                state = state with { Background = parsed };
                            }

                            i += consumed;
                        }
                    }
                    break;
            }

            i++;
        }
    }

    private static bool TryParseExtendedColor(List<int> codes, int codeIndex, out int consumedExtraCodes, out AnsiColorSpec color)
    {
        consumedExtraCodes = 0;
        color = AnsiColorSpec.Default;

        if (codeIndex + 1 >= codes.Count)
        {
            return false;
        }

        int mode = codes[codeIndex + 1];
        if (mode == 5)
        {
            if (codeIndex + 2 >= codes.Count)
            {
                return false;
            }

            color = AnsiColorSpec.FromIndexed256(codes[codeIndex + 2]);
            consumedExtraCodes = 2;
            return true;
        }

        if (mode == 2)
        {
            if (codeIndex + 4 >= codes.Count)
            {
                return false;
            }

            color = AnsiColorSpec.FromTrueColor(
                codes[codeIndex + 2],
                codes[codeIndex + 3],
                codes[codeIndex + 4]);

            consumedExtraCodes = 4;
            return true;
        }

        return false;
    }
}
