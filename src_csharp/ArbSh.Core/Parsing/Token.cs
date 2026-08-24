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
        Identifier,     // Command name or general argument (e.g., الأوامر, file.txt, عربي)
        ParameterName,  // Parameter name (e.g., -Name, -الأمر)
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
        /// <summary>نوع الرمز.</summary>
        public TokenType Type { get; }

        /// <summary>القيمة الخام للرمز.</summary>
        public string Value { get; }

        /// <summary>موضع بداية الرمز في نص مرحلة خط الأنابيب.</summary>
        public int Start { get; }

        /// <summary>طول الرمز بوحدات UTF-16.</summary>
        public int Length { get; }

        /// <summary>الموضع التالي لنهاية الرمز.</summary>
        public int End => Start + Length;

        /// <summary>
        /// ينشئ رمزًا مع موضعه للحفاظ على حدود الوسائط بعد حذف المسافات.
        /// </summary>
        public Token(TokenType type, string value, int start = 0)
        {
            Type = type;
            Value = value;
            Start = start;
            Length = value.Length;
        }

        public override string ToString() => $"[{Type}: '{Value}']";
    }
}

