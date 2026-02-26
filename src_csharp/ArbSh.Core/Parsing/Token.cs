using System;

namespace ArbSh.Core.Parsing
{
    /// <summary>
    /// Represents the different types of tokens recognized by the tokenizer.
    /// </summary>
    public enum TokenType
    {
        Unknown,        // Unrecognized character sequence
        Whitespace,     // Whitespace characters (usually ignored)
        Comment,        // A comment starting with # (usually ignored)
        Identifier,     // Command name or general argument (e.g., Get-Command, file.txt, عربي)
        ParameterName,  // Parameter name (e.g., -Name, -الاسم)
        Variable,       // Variable reference (e.g., $testVar, $متغير)
        StringLiteralDQ,// Double-quoted string literal ("...")
        StringLiteralSQ,// Single-quoted string literal ('...')
        Operator,       // Operators like |, >, >>, 2>, etc.
        SubExpressionStart, // $(
        SubExpressionEnd,   // )
        GroupStart,     // (
        GroupEnd,       // )
        Separator,      // ;
        TypeLiteral     // e.g., [int], [string], [MyNamespace.MyClass]
        // Add other types as needed
    }

    /// <summary>
    /// Represents a single token identified by the tokenizer.
    /// </summary>
    public readonly struct Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        // TODO: Add position/line info later if needed for better error reporting

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString() => $"[{Type}: '{Value}']";
    }
}

