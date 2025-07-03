using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using ArbSh.Console.I18n;
using static ArbSh.Console.I18n.BidiAlgorithm;

namespace ArbSh.Test
{
    /// <summary>
    /// Test framework for running Unicode BidiTest.txt conformance tests
    /// </summary>
    public class BidiTestFramework
    {
        private readonly ITestOutputHelper _output;

        public BidiTestFramework(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Represents a parsed BidiTest.txt test case
        /// </summary>
        public class BidiTestCase
        {
            public List<string> BidiClasses { get; set; } = new List<string>();
            public int ParagraphLevelBitset { get; set; }
            public List<int?> ExpectedLevels { get; set; } = new List<int?>();
            public List<int> ExpectedReorder { get; set; } = new List<int>();
            public int LineNumber { get; set; }
            public string OriginalLine { get; set; } = "";
        }

        /// <summary>
        /// Parses BidiTest.txt format and returns test cases
        /// </summary>
        public static List<BidiTestCase> ParseBidiTestFile(string filePath)
        {
            var testCases = new List<BidiTestCase>();
            var lines = File.ReadAllLines(filePath);
            
            List<int?> currentLevels = null;
            List<int> currentReorder = null;
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                var trimmedLine = line.Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                if (trimmedLine.StartsWith("@Levels:"))
                {
                    // Parse levels line: @Levels: 0 1 x 2
                    var levelsStr = trimmedLine.Substring(8).Trim();
                    currentLevels = ParseLevels(levelsStr);
                }
                else if (trimmedLine.StartsWith("@Reorder:"))
                {
                    // Parse reorder line: @Reorder: 1 0 3 2
                    var reorderStr = trimmedLine.Substring(9).Trim();
                    currentReorder = ParseReorder(reorderStr);
                }
                else if (trimmedLine.StartsWith("@"))
                {
                    // Ignore other @ lines for forward compatibility
                    continue;
                }
                else if (trimmedLine.Contains(";"))
                {
                    // Parse test case line: L R AL; 7
                    var parts = trimmedLine.Split(';');
                    if (parts.Length == 2)
                    {
                        var bidiClassesStr = parts[0].Trim();
                        var bitsetStr = parts[1].Trim();
                        
                        var bidiClasses = bidiClassesStr.Split(new[] { ' ', '\t' }, 
                            StringSplitOptions.RemoveEmptyEntries).ToList();
                        
                        if (int.TryParse(bitsetStr, out int bitset))
                        {
                            var testCase = new BidiTestCase
                            {
                                BidiClasses = bidiClasses,
                                ParagraphLevelBitset = bitset,
                                ExpectedLevels = currentLevels?.ToList() ?? new List<int?>(),
                                ExpectedReorder = currentReorder?.ToList() ?? new List<int>(),
                                LineNumber = lineNumber,
                                OriginalLine = line
                            };
                            testCases.Add(testCase);
                        }
                    }
                }
            }

            return testCases;
        }

        /// <summary>
        /// Parses levels string like "0 1 x 2" into list of nullable ints
        /// </summary>
        private static List<int?> ParseLevels(string levelsStr)
        {
            if (string.IsNullOrEmpty(levelsStr))
                return new List<int?>();

            return levelsStr.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s == "x" ? (int?)null : int.Parse(s))
                .ToList();
        }

        /// <summary>
        /// Parses reorder string like "1 0 3 2" into list of ints
        /// </summary>
        private static List<int> ParseReorder(string reorderStr)
        {
            if (string.IsNullOrEmpty(reorderStr))
                return new List<int>();

            return reorderStr.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
        }

        /// <summary>
        /// Maps BidiTest.txt bidi class names to our BidiCharacterType enum values
        /// </summary>
        private static BidiCharacterType MapBidiClass(string bidiClassName)
        {
            return bidiClassName switch
            {
                "L" => BidiCharacterType.L,
                "R" => BidiCharacterType.R,
                "AL" => BidiCharacterType.AL,
                "EN" => BidiCharacterType.EN,
                "ES" => BidiCharacterType.ES,
                "ET" => BidiCharacterType.ET,
                "AN" => BidiCharacterType.AN,
                "CS" => BidiCharacterType.CS,
                "NSM" => BidiCharacterType.NSM,
                "BN" => BidiCharacterType.BN,
                "B" => BidiCharacterType.B,
                "S" => BidiCharacterType.S,
                "WS" => BidiCharacterType.WS,
                "ON" => BidiCharacterType.ON,
                "LRE" => BidiCharacterType.LRE,
                "LRO" => BidiCharacterType.LRO,
                "RLE" => BidiCharacterType.RLE,
                "RLO" => BidiCharacterType.RLO,
                "PDF" => BidiCharacterType.PDF,
                "LRI" => BidiCharacterType.LRI,
                "RLI" => BidiCharacterType.RLI,
                "FSI" => BidiCharacterType.FSI,
                "PDI" => BidiCharacterType.PDI,
                _ => throw new ArgumentException($"Unknown bidi class: {bidiClassName}")
            };
        }

        /// <summary>
        /// Creates a test string from bidi classes using representative characters
        /// </summary>
        private static string CreateTestString(List<string> bidiClasses)
        {
            var sb = new StringBuilder();
            foreach (var bidiClass in bidiClasses)
            {
                // Use representative characters for each bidi class
                char testChar = bidiClass switch
                {
                    "L" => 'A',      // Latin letter
                    "R" => '\u05D0', // Hebrew Alef
                    "AL" => '\u0627', // Arabic Alef
                    "EN" => '1',     // European Number
                    "ES" => '+',     // European Separator
                    "ET" => '$',     // European Terminator
                    "AN" => '\u0660', // Arabic Number
                    "CS" => ',',     // Common Separator
                    "NSM" => '\u0300', // Non-spacing Mark
                    "BN" => '\u200B', // Boundary Neutral (ZWSP)
                    "B" => '\n',     // Block Separator
                    "S" => '\t',     // Segment Separator
                    "WS" => ' ',     // Whitespace
                    "ON" => '!',     // Other Neutral
                    "LRE" => '\u202A', // Left-to-Right Embedding
                    "LRO" => '\u202D', // Left-to-Right Override
                    "RLE" => '\u202B', // Right-to-Left Embedding
                    "RLO" => '\u202E', // Right-to-Left Override
                    "PDF" => '\u202C', // Pop Directional Format
                    "LRI" => '\u2066', // Left-to-Right Isolate
                    "RLI" => '\u2067', // Right-to-Left Isolate
                    "FSI" => '\u2068', // First Strong Isolate
                    "PDI" => '\u2069', // Pop Directional Isolate
                    _ => '?'
                };
                sb.Append(testChar);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Runs a single BidiTest case and returns success/failure
        /// </summary>
        public bool RunTestCase(BidiTestCase testCase)
        {
            try
            {
                // Create test string from bidi classes
                var testString = CreateTestString(testCase.BidiClasses);
                
                // Test each paragraph level specified in the bitset
                var paragraphLevels = GetParagraphLevelsFromBitset(testCase.ParagraphLevelBitset);
                
                foreach (var paragraphLevel in paragraphLevels)
                {
                    // Run BiDi algorithm
                    var runs = BidiAlgorithm.ProcessRuns(testString, paragraphLevel);

                    // Convert runs to levels array
                    var levels = ConvertRunsToLevels(runs, testString.Length);

                    // Verify levels (if expected levels are provided)
                    if (testCase.ExpectedLevels.Count > 0)
                    {
                        if (!VerifyLevels(levels, testCase.ExpectedLevels))
                        {
                            _output?.WriteLine($"FAIL: Line {testCase.LineNumber} - Level mismatch for paragraph level {paragraphLevel}");
                            _output?.WriteLine($"  Input: {string.Join(" ", testCase.BidiClasses)}");
                            _output?.WriteLine($"  Expected: {string.Join(" ", testCase.ExpectedLevels.Select(l => l?.ToString() ?? "x"))}");
                            _output?.WriteLine($"  Actual:   {string.Join(" ", levels.Select(l => l.ToString()))}");
                            return false;
                        }
                    }

                    // Verify reordering (if expected reorder is provided)
                    if (testCase.ExpectedReorder.Count > 0)
                    {
                        var actualReorder = GetVisualOrder(levels);
                        if (!VerifyReorder(actualReorder, testCase.ExpectedReorder))
                        {
                            _output?.WriteLine($"FAIL: Line {testCase.LineNumber} - Reorder mismatch for paragraph level {paragraphLevel}");
                            _output?.WriteLine($"  Input: {string.Join(" ", testCase.BidiClasses)}");
                            _output?.WriteLine($"  Expected: {string.Join(" ", testCase.ExpectedReorder)}");
                            _output?.WriteLine($"  Actual:   {string.Join(" ", actualReorder)}");
                            return false;
                        }
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _output?.WriteLine($"ERROR: Line {testCase.LineNumber} - Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extracts paragraph levels from bitset (1=auto-LTR, 2=LTR, 4=RTL)
        /// </summary>
        private static List<int> GetParagraphLevelsFromBitset(int bitset)
        {
            var levels = new List<int>();
            if ((bitset & 1) != 0) levels.Add(-1); // Auto-LTR
            if ((bitset & 2) != 0) levels.Add(0);  // LTR
            if ((bitset & 4) != 0) levels.Add(1);  // RTL
            return levels;
        }

        /// <summary>
        /// Converts BidiRun list to levels array
        /// </summary>
        private static List<int> ConvertRunsToLevels(List<BidiRun> runs, int textLength)
        {
            var levels = new int[textLength];

            foreach (var run in runs)
            {
                for (int i = run.Start; i < run.Start + run.Length && i < textLength; i++)
                {
                    levels[i] = run.Level;
                }
            }

            return levels.ToList();
        }

        /// <summary>
        /// Verifies that actual levels match expected levels (accounting for 'x' values)
        /// </summary>
        private static bool VerifyLevels(List<int> actual, List<int?> expected)
        {
            if (actual.Count != expected.Count)
                return false;

            for (int i = 0; i < actual.Count; i++)
            {
                if (expected[i].HasValue && actual[i] != expected[i].Value)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Verifies that actual reorder matches expected reorder
        /// </summary>
        private static bool VerifyReorder(List<int> actual, List<int> expected)
        {
            return actual.SequenceEqual(expected);
        }

        /// <summary>
        /// Computes visual order from levels (simplified version)
        /// </summary>
        private static List<int> GetVisualOrder(List<int> levels)
        {
            // Create list of (index, level) pairs
            var indexedLevels = levels.Select((level, index) => new { Index = index, Level = level }).ToList();
            
            // Sort by level (even levels left-to-right, odd levels right-to-left)
            // This is a simplified version - full implementation would need proper L2 rule
            var sorted = indexedLevels.OrderBy(x => x.Level).ThenBy(x => x.Index).ToList();
            
            return sorted.Select(x => x.Index).ToList();
        }
    }
}
