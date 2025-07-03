using System;

namespace ArbSh.Console.Commands
{
    /// <summary>
    /// Test cmdlet for demonstrating type literal functionality.
    /// Accepts various parameter types to test type literal conversion.
    /// </summary>
    public class TestTypeLiteralCmdlet : CmdletBase
    {
        [Parameter(Position = 0, HelpMessage = "An integer value.")]
        public int IntValue { get; set; }

        [Parameter(Position = 1, HelpMessage = "A string value.")]
        public string? StringValue { get; set; }

        [Parameter(Position = 2, HelpMessage = "A boolean value.")]
        public bool BoolValue { get; set; }

        [Parameter(Position = 3, HelpMessage = "A double value.")]
        public double DoubleValue { get; set; }

        [Parameter(Position = 4, HelpMessage = "A DateTime value.")]
        public DateTime DateTimeValue { get; set; }

        [Parameter(HelpMessage = "An array of integers.")]
        public int[]? IntArray { get; set; }

        [Parameter(HelpMessage = "A ConsoleColor value.")]
        public ConsoleColor ColorValue { get; set; }

        public override void ProcessRecord(PipelineObject? input)
        {
            // This cmdlet primarily works with parameters bound before ProcessRecord is called.
            // We'll output the bound values in EndProcessing.
        }

        public override void EndProcessing()
        {
            WriteObject($"IntValue: {IntValue} (Type: {IntValue.GetType().Name})");
            WriteObject($"StringValue: '{StringValue}' (Type: {StringValue?.GetType().Name ?? "null"})");
            WriteObject($"BoolValue: {BoolValue} (Type: {BoolValue.GetType().Name})");
            WriteObject($"DoubleValue: {DoubleValue} (Type: {DoubleValue.GetType().Name})");
            WriteObject($"DateTimeValue: {DateTimeValue} (Type: {DateTimeValue.GetType().Name})");
            WriteObject($"ColorValue: {ColorValue} (Type: {ColorValue.GetType().Name})");

            if (IntArray != null && IntArray.Length > 0)
            {
                WriteObject($"IntArray: [{string.Join(", ", IntArray)}] (Type: {IntArray.GetType().Name}, Length: {IntArray.Length})");
            }
            else
            {
                WriteObject("IntArray: null or empty");
            }
        }
    }
}
