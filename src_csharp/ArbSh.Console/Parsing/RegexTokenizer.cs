using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq; // For Select

namespace ArbSh.Console.Parsing
{
    /// <summary>
    /// Provides functionality to tokenize an input string based on regular expressions.
    /// </summary>
    public static class RegexTokenizer
    {
        // Define token patterns using named capture groups. Order matters!
        // More specific patterns (like multi-char operators) should come before general ones.
        private static readonly List<(TokenType Type, string Pattern)> TokenDefinitions = new List<(TokenType, string)>
        {
            // Whitespace (ignored later, but needed for splitting)
            (TokenType.Whitespace,      @"(?<Whitespace>\s+)"),
            // Comment (ignored later)
            (TokenType.Comment,         @"(?<Comment>#.*)"), // Match # to end of line

            // Literals
            (TokenType.StringLiteralDQ, @"(?<StringLiteralDQ>""(?:\\.|[^""\\])*"")"), // Double quotes, handles escapes
            (TokenType.StringLiteralSQ, @"(?<StringLiteralSQ>'(?:[^']*)')"),       // Single quotes, literal content

            // Variables
            (TokenType.Variable,        @"(?<Variable>\$[\p{L}_][\p{L}\p{N}_]*)"), // $ followed by identifier chars

            // Parameters
            (TokenType.ParameterName,   @"(?<ParameterName>-[\p{L}_][\p{L}\p{N}_-]*)"), // - followed by identifier chars

            // Operators (Order longer/more specific ones first!)
            // Stream Redirection (e.g., >>&1, >&1, 2>>&1, 2>&1, >&2) - Capture the whole operator
            (TokenType.Operator,        @"(?<Operator>\d*>>&?\d)"), // e.g., 2>>&1, >>&1, >>&2
            (TokenType.Operator,        @"(?<Operator>\d*>&?\d)"),  // e.g., 2>&1, >&1, >&2
            // File Redirection (e.g., 2>>, >>, 2>, >) - Must come AFTER stream redirection
            (TokenType.Operator,        @"(?<Operator>\d*>>)"),     // e.g., 2>>, >>
            (TokenType.Operator,        @"(?<Operator>\d*>)"),      // e.g., 2>, >
            // Input Redirection
            (TokenType.Operator,        @"(?<Operator><)"),           // < Input redirection
            // Other Operators
            (TokenType.SubExpressionStart,@"(?<SubExpressionStart>\$\()"), // $( Subexpression start
            (TokenType.Separator,       @"(?<Separator>;)"),          // ;
            (TokenType.Operator,        @"(?<Operator>\|)"),           // | Pipe
            (TokenType.GroupStart,      @"(?<GroupStart>\()"),         // ( Grouping
            (TokenType.GroupEnd,        @"(?<GroupEnd>\))"),           // )
            (TokenType.SubExpressionEnd,@"(?<SubExpressionEnd>\))"),     // ) - Same as GroupEnd, context determines meaning
            
            // Type Literals (e.g., [int], [System.ConsoleColor])
            (TokenType.TypeLiteral,     @"(?<TypeLiteral>\[\s*[\p{L}_][\p{L}\p{N}_\.]*\s*\])"),
            // TODO: Add other operators like &, etc.

            // Identifiers / Arguments (Must come after parameters/variables/type literals)
            // Allows letters (inc. Arabic), numbers, underscore, hyphen. Also allows '.', '\', '/' for paths.
            (TokenType.Identifier,      @"(?<Identifier>[\p{L}_\./\\-][\p{L}\p{N}_\./\\-]*)"), // Starts with letter, _, ., /, \, - followed by more

            // Unknown (Catch-all for error reporting) - Must be last
            (TokenType.Unknown,         @"(?<Unknown>.)")
        };

        // Combine patterns into a single regex
        private static readonly Regex TokenRegex = new Regex(
            string.Join("|", TokenDefinitions.Select(td => td.Pattern)),
            RegexOptions.Compiled | RegexOptions.ExplicitCapture // Improve performance
        );

        /// <summary>
        /// Tokenizes the input string using the defined regex patterns.
        /// </summary>
        /// <param name="input">The input string to tokenize.</param>
        /// <returns>A list of recognized tokens.</returns>
        public static List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            var matches = TokenRegex.Matches(input);
            int currentPosition = 0;

            foreach (Match match in matches)
            {
                // Check for gaps between matches (indicates unrecognized characters)
                if (match.Index > currentPosition)
                {
                    string gapText = input.Substring(currentPosition, match.Index - currentPosition);
                    System.Console.WriteLine($"WARN (Tokenizer): Unrecognized characters: '{gapText}'");
                    // Optionally add these as Unknown tokens
                    foreach(char c in gapText)
                    {
                         tokens.Add(new Token(TokenType.Unknown, c.ToString()));
                    }
                }

                // Find which named group matched
                bool matched = false;
                foreach (var def in TokenDefinitions)
                {
                    // Extract group name from pattern (e.g., "Whitespace" from "(?<Whitespace>\s+)")
                    string groupName = def.Pattern.Substring(3, def.Pattern.IndexOf('>') - 3); // Assumes pattern "(?<Name>...)"

                    if (match.Groups[groupName].Success)
                    {
                        // Ignore whitespace and comments
                        if (def.Type != TokenType.Whitespace && def.Type != TokenType.Comment)
                        {
                            string value = match.Value;
                            // TODO: Add post-processing for strings (remove quotes, handle escapes) if needed here
                            // For now, just add the raw matched value.
                            tokens.Add(new Token(def.Type, value));
                        }
                        matched = true;
                        break; // Move to the next match
                    }
                }

                if (!matched)
                {
                    // This shouldn't happen if the Unknown pattern is last and correct
                    System.Console.WriteLine($"ERROR (Tokenizer): Match found but no group matched? Value: '{match.Value}'");
                    tokens.Add(new Token(TokenType.Unknown, match.Value));
                }

                currentPosition = match.Index + match.Length;
            }

            // Check if the entire string was consumed
            if (currentPosition < input.Length)
            {
                 string remainingText = input.Substring(currentPosition);
                 System.Console.WriteLine($"WARN (Tokenizer): Unconsumed trailing characters: '{remainingText}'");
                 // Optionally add these as Unknown tokens
                 foreach(char c in remainingText)
                 {
                      tokens.Add(new Token(TokenType.Unknown, c.ToString()));
                 }
            }

            return tokens;
        }
    }
}
