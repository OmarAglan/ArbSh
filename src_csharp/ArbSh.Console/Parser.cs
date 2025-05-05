using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq; // Needed for Skip
using System.Text; // Needed for StringBuilder
using System.Text.RegularExpressions; // Needed for Regex.Match
using ArbSh.Console.Parsing; // Add using for Token types

namespace ArbSh.Console
{
    /// <summary>
    /// Responsible for parsing user input strings into executable commands/structures.
    /// </summary>
    public static class Parser
    {
        // Placeholder for variable storage (replace with proper session state later)
        // TODO: Implement proper session state for variables
        private static Dictionary<string, string> _variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "testVar", "Value from $testVar!" },
            { "pathExample", "C:\\Users" },
            { "emptyVar", "" }
        };

        // Basic variable lookup (replace with proper session state access)
        private static string GetVariableValue(string variableName)
        {
            // Basic lookup, returns empty string if not found (like PowerShell)
            return _variables.TryGetValue(variableName, out var value) ? value : string.Empty;
        }

        // NOTE: Old state machine tokenizer and helper methods removed (IsArabicLetterChar, IsValidIdentifierChar, etc.)
        //       as tokenization is now handled by RegexTokenizer.

        /// <summary>
        /// Parses a line of input into a list of statements, where each statement is a list of commands for a pipeline.
        /// </summary>
        /// <param name="inputLine">The raw input line from the user.</param>
        /// <returns>A list where each element is a list of ParsedCommand objects representing a single statement's pipeline.</returns>
        public static List<List<ParsedCommand>> Parse(string inputLine)
        {
            System.Console.WriteLine($"DEBUG (Parser): Parsing '{inputLine}'...");
            var allStatementsCommands = new List<List<ParsedCommand>>();
            var statementBuilder = new StringBuilder();
            bool inDoubleQuotes = false;
            bool inSingleQuotes = false; // Add tracking for single quotes
            int start = 0;

            for (int i = 0; i < inputLine.Length; i++)
            {
                char c = inputLine[i];

                // Handle comments: If # is encountered outside quotes, ignore the rest of the line
                if (c == '#' && !inDoubleQuotes && !inSingleQuotes)
                {
                    // If # is the first char of the potential statement, ignore the whole line segment
                    if (i == start || string.IsNullOrWhiteSpace(inputLine.Substring(start, i - start)))
                    {
                        start = inputLine.Length; // Effectively skip the rest
                    }
                    break; // Ignore rest of the current line segment being processed
                }

                // Handle escaping - skip next char if escaped (only applies outside single quotes)
                if (c == '\\' && !inSingleQuotes && i + 1 < inputLine.Length)
                {
                    i++;
                    continue; // Skip escape character logic for statement splitting
                }

                // Toggle quote states
                if (c == '"' && !inSingleQuotes)
                {
                    inDoubleQuotes = !inDoubleQuotes;
                }
                else if (c == '\'' && !inDoubleQuotes) // Add single quote handling
                {
                    inSingleQuotes = !inSingleQuotes;
                }

                // Split statements only if outside *both* types of quotes
                if (c == ';' && !inDoubleQuotes && !inSingleQuotes)
                {
                    string statement = inputLine.Substring(start, i - start).Trim();
                    // Ignore empty statements or statements that were just comments
                    if (!string.IsNullOrWhiteSpace(statement) && !statement.TrimStart().StartsWith("#"))
                    {
                        allStatementsCommands.Add(ParseSingleStatement(statement));
                    }
                    start = i + 1; // Start next statement after the semicolon
                }
            }

            // Add the last statement (or the only statement if no semicolons)
            string lastStatement = inputLine.Substring(start).Trim();
            // Ignore empty statements or statements that were just comments
            if (!string.IsNullOrWhiteSpace(lastStatement) && !lastStatement.TrimStart().StartsWith("#"))
            {
                // Also ignore if the remaining part starts with # after trimming whitespace
                int commentIndex = lastStatement.IndexOf('#');
                if (commentIndex == 0) { /* Ignore */ }
                else if (commentIndex > 0)
                {
                    // If comment is present but not at start, parse the part before it
                    string beforeComment = lastStatement.Substring(0, commentIndex).Trim();
                    if (!string.IsNullOrWhiteSpace(beforeComment))
                    {
                        allStatementsCommands.Add(ParseSingleStatement(beforeComment));
                    }
                }
                else
                {
                    // No comment found
                    allStatementsCommands.Add(ParseSingleStatement(lastStatement));
                }
            }


            // TODO: Handle unterminated quotes at the statement level?
            if (inDoubleQuotes || inSingleQuotes)
            {
                System.Console.WriteLine("WARN (Parser): Unterminated quote detected at end of input line.");
                // Potentially throw error or try to recover?
            }


            System.Console.WriteLine($"DEBUG (Parser): Parsed into {allStatementsCommands.Count} statement(s).");
            return allStatementsCommands;
        }

        /// <summary>
        /// Parses a single statement (which might contain a pipeline) into a list of commands.
        /// </summary>
        private static List<ParsedCommand> ParseSingleStatement(string statementInput)
        {
            System.Console.WriteLine($"DEBUG (Parser): Processing statement: '{statementInput}'");
            var commandsInStatement = new List<ParsedCommand>();
            int stageStart = 0;
            bool inDoubleQuotes = false; // Track quotes specifically for pipeline splitting
            bool inSingleQuotes = false; // Track single quotes for pipeline splitting

            for (int i = 0; i < statementInput.Length; i++)
            {
                char c = statementInput[i];

                // Handle escaping (only outside single quotes)
                if (c == '\\' && !inSingleQuotes && i + 1 < statementInput.Length)
                {
                    i++; // Skip escaped character for pipeline splitting logic
                    continue;
                }

                // Toggle quote states
                if (c == '"' && !inSingleQuotes)
                {
                    inDoubleQuotes = !inDoubleQuotes;
                }
                else if (c == '\'' && !inDoubleQuotes)
                {
                    inSingleQuotes = !inSingleQuotes;
                }

                // Split pipeline only if outside *both* types of quotes
                if (c == '|' && !inDoubleQuotes && !inSingleQuotes)
                {
                    string stageInput = statementInput.Substring(stageStart, i - stageStart).Trim();
                    if (!string.IsNullOrWhiteSpace(stageInput))
                    {
                        ParsedCommand? command = ParseSinglePipelineStage(stageInput);
                        if (command != null) commandsInStatement.Add(command);
                    }
                    stageStart = i + 1; // Start next stage after the pipe
                }
            }

            // Process the last stage
            string lastStageInput = statementInput.Substring(stageStart).Trim();
            if (!string.IsNullOrWhiteSpace(lastStageInput))
            {
                ParsedCommand? command = ParseSinglePipelineStage(lastStageInput);
                if (command != null) commandsInStatement.Add(command);
            }

            // TODO: Handle unterminated quotes within the statement?

            System.Console.WriteLine($"DEBUG (Parser): Statement parsed into {commandsInStatement.Count} pipeline stage(s).");
            return commandsInStatement;
        }

        /// <summary>
        /// Parses a single pipeline stage string into a ParsedCommand object.
        /// Handles tokenization, redirection, arguments, parameters.
        /// </summary>
        private static ParsedCommand? ParseSinglePipelineStage(string stageInput)
        {
            // Tokenize the current pipeline stage input using the new Regex Tokenizer
            List<Token> tokens = RegexTokenizer.Tokenize(stageInput);
            if (tokens.Count == 0) return null;

            // First token should be the command name (Identifier)
            if (tokens[0].Type != TokenType.Identifier)
            {
                // Handle error: Expected a command name identifier
                System.Console.WriteLine($"ERROR (Parser): Expected command name, but got token type {tokens[0].Type} ('{tokens[0].Value}')");
                return null; // Or throw exception
            }
            string commandName = tokens[0].Value;
            List<Token> remainingTokens = tokens.Skip(1).ToList(); // Start with tokens after command name

            // Create the command object early so we can add redirections
            var parsedCommand = new ParsedCommand(commandName, new List<object>(), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)); // Arguments list is List<object>

            // --- Advanced Redirection Parsing ---
            // Loop through tokens to find and process *all* redirection operators
            // We need to iterate carefully as we remove items from the list
            for (int i = 0; i < remainingTokens.Count; /* No increment here */)
            {
                Token currentToken = remainingTokens[i];
                bool processed = false; // Flag to indicate if the token was processed as part of a redirection

                if (currentToken.Type == TokenType.Operator)
                {
                    string opValue = currentToken.Value;
                    ParsedCommand.RedirectionInfo? redirection = TryParseRedirectionOperator(opValue, remainingTokens, i);

                    if (redirection != null)
                    {
                        parsedCommand.AddRedirection(redirection.Value);
                        // Determine how many tokens were consumed (1 for stream, 2 for file)
                        int tokensToRemove = (redirection.Value.TargetType == ParsedCommand.RedirectionTargetType.StreamHandle) ? 1 : 2;

                        // Remove consumed tokens (in reverse index order)
                        if (tokensToRemove == 2) remainingTokens.RemoveAt(i + 1);
                        remainingTokens.RemoveAt(i);
                        processed = true;
                        // Do NOT increment i, list shifted, restart check from current index
                        continue;
                    }
                    // Handle cases where the token looked like an operator but wasn't valid redirection
                    else if (opValue.Contains(">")) // If it looked like redirection but failed parsing
                    {
                         System.Console.WriteLine($"WARN (Parser): Failed to parse redirection for operator token '{opValue}'.");
                         remainingTokens.RemoveAt(i); // Remove the invalid operator token
                         processed = true;
                         continue; // Restart check from current index
                    }
                }

                // If the token wasn't processed as redirection, move to the next one
                if (!processed)
                {
                    i++;
                }
            }


            // --- Argument/Parameter Parsing (using remaining tokens AFTER redirection removal) ---
            List<object> arguments = parsedCommand.Arguments; // Get the arguments list (List<object>)
            Dictionary<string, string> parameters = parsedCommand.Parameters; // Get the parameters dictionary

            for (int i = 0; i < remainingTokens.Count; i++)
            {
                Token currentToken = remainingTokens[i];

                // Check for parameter name
                if (currentToken.Type == TokenType.ParameterName)
                {
                    string paramName = currentToken.Value;
                    string? paramValue = null;

                    // Check if the next token exists and is NOT another parameter name
                    if (i + 1 < remainingTokens.Count && remainingTokens[i + 1].Type != TokenType.ParameterName)
                    {
                        // Value can be Identifier, StringLiteral, Variable, etc. Use its Value.
                        paramValue = remainingTokens[i + 1].Value;
                        i++; // Consume the value token
                    }
                    parameters[paramName] = paramValue ?? string.Empty; // Store even if value is null (for switch parameters)
                }
                // --- SubExpression Parsing ---
                else if (currentToken.Type == TokenType.SubExpressionStart) // Check for subexpression start token '$('
                {
                    var subExpressionTokens = new List<Token>(); // Store Tokens within subexpression
                    int parenNestingLevel = 1; // Start at 1 due to the opening '$('
                    i++; // Move past the '$(' token

                    while (i < remainingTokens.Count)
                    {
                        Token subToken = remainingTokens[i];
                        // Use GroupStart/SubExpressionStart and GroupEnd/SubExpressionEnd for nesting
                        if (subToken.Type == TokenType.GroupStart || subToken.Type == TokenType.SubExpressionStart)
                        {
                            parenNestingLevel++;
                        }
                        else if (subToken.Type == TokenType.GroupEnd || subToken.Type == TokenType.SubExpressionEnd)
                        {
                            parenNestingLevel--;
                            if (parenNestingLevel == 0)
                            {
                                // Found the matching closing parenthesis
                                break; // Exit the inner loop
                            }
                        }
                        subExpressionTokens.Add(subToken);
                        i++; // Move to the next token within the subexpression
                    }

                    if (parenNestingLevel != 0)
                    {
                        System.Console.WriteLine("WARN (Parser): Unterminated subexpression '$()' found.");
                        // Add collected tokens as raw string arguments for now?
                        arguments.AddRange(subExpressionTokens.Select(t => t.Value).Cast<object>());
                    }
                    else
                    {
                        // Successfully parsed subexpression.
                        // Convert sub-tokens back to a string to re-parse recursively.
                        // This is inefficient but simpler for now than directly parsing token list.
                        // TODO: Improve subexpression handling to directly parse token list?
                        string subExpressionInput = string.Join(" ", subExpressionTokens.Select(t => t.Value)); // Reconstruct string
                        System.Console.WriteLine($"DEBUG (Parser): Recursively parsing subexpression content: '{subExpressionInput}'");
                        List<List<ParsedCommand>> subStatements = Parse(subExpressionInput); // Recursive call

                        if (subStatements.Count > 0)
                        {
                            // Add the list of commands from the first statement as the argument.
                            arguments.Add(subStatements[0]); // Add List<ParsedCommand> to List<object>
                            System.Console.WriteLine($"DEBUG (Parser): Added parsed subexpression (statement 0) as argument.");
                        }
                        else
                        {
                            System.Console.WriteLine($"WARN (Parser): Subexpression '$({subExpressionInput})' parsed into zero statements.");
                            arguments.Add(new List<ParsedCommand>()); // Add empty list
                        }
                        // 'i' is already positioned after the closing ')' due to the loop structure
                    }
                }
                 else // It's a regular argument (Identifier, StringLiteral, Variable, etc.)
                 {
                     // Check if it's a variable token that needs expansion
                     if (currentToken.Type == TokenType.Variable)
                     {
                         // Extract variable name (remove leading '$')
                         string varName = currentToken.Value.Substring(1);
                         string varValue = GetVariableValue(varName);
                         arguments.Add(varValue); // Add the expanded value
                         System.Console.WriteLine($"DEBUG (Parser): Expanded variable '{currentToken.Value}' to '{varValue}' during argument parsing.");
                     }
                     // TODO: Handle StringLiterals - remove quotes, process escapes?
                     // else if (currentToken.Type == TokenType.StringLiteralDQ) { ... }
                     // else if (currentToken.Type == TokenType.StringLiteralSQ) { ... }
                     else
                     {
                         // Add other token types' values directly (Identifier, StringLiteral for now)
                         arguments.Add(currentToken.Value);
                     }
                 }
            }

            // Return the command object populated earlier
            return parsedCommand;
        }


        /// <summary>
        /// Attempts to parse a redirection operator token and its potential target.
        /// </summary>
        /// <param name="operatorTokenValue">The string value of the operator token (e.g., "2>", ">&1").</param>
        /// <param name="remainingTokens">The list of remaining tokens after the command name.</param>
        /// <param name="operatorIndex">The index of the operator token in remainingTokens.</param>
        /// <returns>A RedirectionInfo struct if parsing is successful, otherwise null.</returns>
        private static ParsedCommand.RedirectionInfo? TryParseRedirectionOperator(string operatorTokenValue, List<Token> remainingTokens, int operatorIndex)
        {
            int sourceHandle = 1; // Default stdout
            bool append = operatorTokenValue.Contains(">>");
            bool targetIsStream = operatorTokenValue.EndsWith("&");

            // --- Determine Source Handle ---
            string handlePart = "";
            int operatorSymbolIndex = operatorTokenValue.IndexOf('>');
            if (operatorSymbolIndex > 0) // Check if there are digits before '>'
            {
                handlePart = operatorTokenValue.Substring(0, operatorSymbolIndex);
                if (!int.TryParse(handlePart, out sourceHandle))
                {
                    System.Console.WriteLine($"WARN (Parser): Invalid source handle '{handlePart}' in redirection operator '{operatorTokenValue}'. Defaulting to 1.");
                    sourceHandle = 1; // Default on parse error
                }
            }
            else
            {
                 sourceHandle = 1; // Default if '>' is the first char
            }
             // Simple override: if it starts with 2 and no other digit, assume stderr
            if (operatorTokenValue.StartsWith("2>") || operatorTokenValue.StartsWith("2>>")) sourceHandle = 2;


            // --- Determine Target ---
            // Note: The Regex patterns in RegexTokenizer should now capture the full operator like "2>&1" or ">&2"
            if (operatorTokenValue.Contains("&")) // Check if it's potentially a stream redirection
            {
                 // Use Regex to extract source (optional) and target handles
                 // Pattern: Optional digits (\d*), followed by > or >>, then & and digits (\d+)
                 Match streamRedirectMatch = Regex.Match(operatorTokenValue, @"^(\d*)>?>(?:&(\d+))$|^(\d*)>(?:&(\d+))$");

                 if (streamRedirectMatch.Success)
                 {
                     // Determine which pattern matched (append >> or overwrite >)
                     string sourceHandleStr = streamRedirectMatch.Groups[1].Success ? streamRedirectMatch.Groups[1].Value : (streamRedirectMatch.Groups[3].Success ? streamRedirectMatch.Groups[3].Value : "");
                     string targetHandleStr = streamRedirectMatch.Groups[2].Success ? streamRedirectMatch.Groups[2].Value : streamRedirectMatch.Groups[4].Value;
                     append = operatorTokenValue.Contains(">>"); // Double check append based on original token

                     // Parse source handle (default to 1 if empty)
                     if (string.IsNullOrEmpty(sourceHandleStr))
                     {
                         sourceHandle = 1;
                     }
                     else if (!int.TryParse(sourceHandleStr, out sourceHandle))
                     {
                         System.Console.WriteLine($"WARN (Parser): Invalid source handle '{sourceHandleStr}' in stream redirection '{operatorTokenValue}'. Defaulting to 1.");
                         sourceHandle = 1;
                     }

                     System.Console.WriteLine($"DEBUG (ParsedCommand): Added stream redirection: {operatorTokenValue} (Source: {sourceHandle}, Target: {targetHandleStr}, Append: {append})");
                     return new ParsedCommand.RedirectionInfo(sourceHandle, ParsedCommand.RedirectionTargetType.StreamHandle, targetHandleStr, append);
                 }
                 else
                 {
                     // Regex didn't match the expected stream redirection format
                     System.Console.WriteLine($"WARN (Parser): Invalid stream redirection operator format '{operatorTokenValue}'. Could not extract target handle.");
                     return null; // Indicate parsing failure
                 }
            }
            else // File target: Expecting next token to be the file path
            {
                if (operatorIndex + 1 < remainingTokens.Count)
                {
                    Token targetToken = remainingTokens[operatorIndex + 1];
                    if (targetToken.Type == TokenType.Identifier || targetToken.Type == TokenType.StringLiteralDQ || targetToken.Type == TokenType.StringLiteralSQ)
                    {
                        // TODO: Handle quotes/escapes in targetToken.Value if necessary
                        string targetPath = targetToken.Value;
                        // Remove quotes from string literals if present
                        if (targetToken.Type == TokenType.StringLiteralDQ || targetToken.Type == TokenType.StringLiteralSQ)
                        {
                             if (targetPath.Length >= 2) // Avoid error on empty strings "" or ''
                                targetPath = targetPath.Substring(1, targetPath.Length - 2);
                        }

                        System.Console.WriteLine($"DEBUG (ParsedCommand): Added file redirection: {operatorTokenValue} {targetPath}");
                        return new ParsedCommand.RedirectionInfo(sourceHandle, ParsedCommand.RedirectionTargetType.FilePath, targetPath, append);
                    }
                    else
                    {
                        System.Console.WriteLine($"WARN (Parser): Unexpected token type '{targetToken.Type}' used as redirection file path target: '{targetToken.Value}'.");
                        return null; // Indicate parsing failure
                    }
                }
                else
                {
                    System.Console.WriteLine($"WARN (Parser): Redirection operator '{operatorTokenValue}' found without a target file path.");
                    return null; // Indicate parsing failure
                }
            }
        }


        // NOTE: Old state machine tokenizer (TokenizeInput, TokenizerStateSM enum, helper methods) removed.
    }
}
