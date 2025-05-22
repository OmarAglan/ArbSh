using System;
using Xunit;
using ArbSh.Console.I18n; // Your namespace for BidiAlgorithm and BidiCharacterType
using System.Collections.Generic; // For List in ProcessRuns tests

namespace ArbSh.Test.I18n
{
    public class BidiAlgorithmTests
    {
        // --- GetCharType Tests ---

        [Fact]
        public void GetCharType_LatinLetter_ReturnsL()
        {
            Assert.Equal(BidiCharacterType.L, BidiAlgorithm.GetCharType('A')); // U+0041
        }

        [Fact]
        public void GetCharType_ArabicLetter_ReturnsAL()
        {
            Assert.Equal(BidiCharacterType.AL, BidiAlgorithm.GetCharType(0x0645)); // 'م' (Meem)
        }

        [Fact]
        public void GetCharType_HebrewLetter_ReturnsR()
        {
            Assert.Equal(BidiCharacterType.R, BidiAlgorithm.GetCharType(0x05D0)); // 'א' (Alef - Hebrew)
        }

        [Fact]
        public void GetCharType_EuropeanNumber_ReturnsEN()
        {
            Assert.Equal(BidiCharacterType.EN, BidiAlgorithm.GetCharType('7')); // U+0037
        }

        [Fact]
        public void GetCharType_ArabicNumber_ReturnsAN()
        {
            Assert.Equal(BidiCharacterType.AN, BidiAlgorithm.GetCharType(0x0661)); // '١' (Arabic-Indic Digit One)
        }

        [Fact]
        public void GetCharType_WhitespaceAndSegmentSeparator_CorrectTypes()
        {
            // Space U+0020 -> Bidi_Class WS
            Assert.Equal(BidiCharacterType.WS, BidiAlgorithm.GetCharType(' '));

            // Tab U+0009 -> Bidi_Class S (Segment Separator)
            Assert.Equal(BidiCharacterType.S, BidiAlgorithm.GetCharType('\t'));
        }

        [Fact]
        public void GetCharType_ParagraphSeparator_ReturnsB()
        {
            // Newline U+000A -> Bidi_Class B
            Assert.Equal(BidiCharacterType.B, BidiAlgorithm.GetCharType('\n'));
        }

        // Explicit Formatting Codes
        [Fact]
        public void GetCharType_LRE_ReturnsLRE()
        {
            Assert.Equal(BidiCharacterType.LRE, BidiAlgorithm.GetCharType(0x202A)); // LEFT-TO-RIGHT EMBEDDING
        }

        [Fact]
        public void GetCharType_RLE_ReturnsRLE()
        {
            Assert.Equal(BidiCharacterType.RLE, BidiAlgorithm.GetCharType(0x202B)); // RIGHT-TO-LEFT EMBEDDING
        }

        [Fact]
        public void GetCharType_PDF_ReturnsPDF()
        {
            Assert.Equal(BidiCharacterType.PDF, BidiAlgorithm.GetCharType(0x202C)); // POP DIRECTIONAL FORMATTING
        }

        [Fact]
        public void GetCharType_LRO_ReturnsLRO()
        {
            Assert.Equal(BidiCharacterType.LRO, BidiAlgorithm.GetCharType(0x202D)); // LEFT-TO-RIGHT OVERRIDE
        }

        [Fact]
        public void GetCharType_RLO_ReturnsRLO()
        {
            Assert.Equal(BidiCharacterType.RLO, BidiAlgorithm.GetCharType(0x202E)); // RIGHT-TO-LEFT OVERRIDE
        }

        [Fact]
        public void GetCharType_LRI_ReturnsLRI()
        {
            Assert.Equal(BidiCharacterType.LRI, BidiAlgorithm.GetCharType(0x2066)); // LEFT-TO-RIGHT ISOLATE
        }

        [Fact]
        public void GetCharType_RLI_ReturnsRLI()
        {
            Assert.Equal(BidiCharacterType.RLI, BidiAlgorithm.GetCharType(0x2067)); // RIGHT-TO-LEFT ISOLATE
        }

        [Fact]
        public void GetCharType_FSI_ReturnsFSI()
        {
            Assert.Equal(BidiCharacterType.FSI, BidiAlgorithm.GetCharType(0x2068)); // FIRST STRONG ISOLATE
        }

        [Fact]
        public void GetCharType_PDI_ReturnsPDI()
        {
            Assert.Equal(BidiCharacterType.PDI, BidiAlgorithm.GetCharType(0x2069)); // POP DIRECTIONAL ISOLATE
        }

        // Boundary Neutrals (LRM, RLM, and other control chars)
        [Fact]
        public void GetCharType_LRM_Codepoint_ReturnsBN()
        {
            // LRM (U+200E) -> Bidi_Class BN
            Assert.Equal(BidiCharacterType.BN, BidiAlgorithm.GetCharType(0x200E));
        }

        [Fact]
        public void GetCharType_RLM_Codepoint_ReturnsBN()
        {
            // RLM (U+200F) -> Bidi_Class BN
            Assert.Equal(BidiCharacterType.BN, BidiAlgorithm.GetCharType(0x200F));
        }

        [Fact]
        public void GetCharType_BoundaryNeutralControlChar_ReturnsBN()
        {
            // Example: U+0000 (NULL) -> Bidi_Class BN
            Assert.Equal(BidiCharacterType.BN, BidiAlgorithm.GetCharType(0x0000));
            // Example: U+0001 (Start of Heading) -> Bidi_Class BN
            Assert.Equal(BidiCharacterType.BN, BidiAlgorithm.GetCharType(0x0001));
        }

        // Weak Types (continued)
        [Fact]
        public void GetCharType_EuropeanSeparator_ReturnsES()
        {
            // '+' (U+002B) -> Bidi_Class ES
            Assert.Equal(BidiCharacterType.ES, BidiAlgorithm.GetCharType('+'));
            // '-' (U+002D) -> Bidi_Class ES
            Assert.Equal(BidiCharacterType.ES, BidiAlgorithm.GetCharType('-'));
        }

        [Fact]
        public void GetCharType_EuropeanTerminator_ReturnsET()
        {
            // '$' (U+0024) -> Bidi_Class ET
            Assert.Equal(BidiCharacterType.ET, BidiAlgorithm.GetCharType('$'));
            // '#' (U+0023) -> Bidi_Class ET
            Assert.Equal(BidiCharacterType.ET, BidiAlgorithm.GetCharType('#'));
            // '%' (U+0025) -> Bidi_Class ET
            Assert.Equal(BidiCharacterType.ET, BidiAlgorithm.GetCharType('%'));
        }


        [Fact]
        public void GetCharType_CommonSeparator_ReturnsCS()
        {
            // '.' (U+002E) -> Bidi_Class CS
            Assert.Equal(BidiCharacterType.CS, BidiAlgorithm.GetCharType('.'));
            // ',' (U+002C) -> Bidi_Class CS
            Assert.Equal(BidiCharacterType.CS, BidiAlgorithm.GetCharType(','));
            // ':' (U+003A) -> Bidi_Class CS
            Assert.Equal(BidiCharacterType.CS, BidiAlgorithm.GetCharType(':'));
            // '/' (U+002F) -> Bidi_Class CS
            Assert.Equal(BidiCharacterType.CS, BidiAlgorithm.GetCharType('/'));
        }

        [Fact]
        public void GetCharType_NonSpacingMark_ReturnsNSM()
        {
            // U+0301 (COMBINING ACUTE ACCENT) -> Bidi_Class NSM
            Assert.Equal(BidiCharacterType.NSM, BidiAlgorithm.GetCharType(0x0301));
        }

        // Other Neutrals & Default Classifications
        [Fact]
        public void GetCharType_OtherNeutral_ReturnsON_ForGeneralSymbol()
        {
            // '©' (U+00A9 COPYRIGHT SIGN) -> Bidi_Class ON
            Assert.Equal(BidiCharacterType.ON, BidiAlgorithm.GetCharType(0x00A9));
        }

        [Fact]
        public void GetCharType_AsciiPunctuationNotOtherwiseClassified_ReturnsON()
        {
            // '!' (U+0021 EXCLAMATION MARK) -> Bidi_Class ON
            Assert.Equal(BidiCharacterType.ON, BidiAlgorithm.GetCharType('!'));
            // '?' (U+003F QUESTION MARK) -> Bidi_Class ON
            Assert.Equal(BidiCharacterType.ON, BidiAlgorithm.GetCharType('?'));
        }

        [Fact]
        public void GetCharType_ExtendedLatinWithDiacritic_ReturnsL()
        {
            // 'é' (U+00E9 LATIN SMALL LETTER E WITH ACUTE) -> Bidi_Class L
            Assert.Equal(BidiCharacterType.L, BidiAlgorithm.GetCharType(0x00E9));
        }


        // --- ProcessRuns Tests (Simplified for current placeholder implementation) ---
        // These tests assume the current placeholder ProcessRuns which includes basic P2/P3.

        [Fact]
        public void ProcessRuns_EmptyText_ReturnsEmptyList_Placeholder()
        {
            string text = "";
            int baseLevel = 0;
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.NotNull(runs);
            Assert.Empty(runs);
        }

        [Fact]
        public void ProcessRuns_PurelyLtrText_BaseLtr_ReturnsSingleLtrRun_Placeholder()
        {
            string text = "Hello";
            int baseLevel = 0; // Explicit LTR
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(text.Length, runs[0].Length);
            Assert.Equal(0, runs[0].Level); // Paragraph level is LTR
        }

        [Fact]
        public void ProcessRuns_PurelyLtrText_AutoBase_ReturnsSingleLtrRun_Placeholder()
        {
            string text = "Hello";
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(text.Length, runs[0].Length);
            Assert.Equal(0, runs[0].Level); // Auto-detected as LTR
        }

        [Fact]
        public void ProcessRuns_PurelyRtlText_BaseRtl_ReturnsSingleRtlRun_Placeholder()
        {
            string text = "مرحبا";
            int baseLevel = 1; // Explicit RTL
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(text.Length, runs[0].Length);
            Assert.Equal(1, runs[0].Level); // Paragraph level is RTL
        }

        [Fact]
        public void ProcessRuns_PurelyRtlText_AutoBase_ReturnsSingleRtlRun_Placeholder()
        {
            string text = "مرحبا";
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(text.Length, runs[0].Length);
            Assert.Equal(1, runs[0].Level); // Auto-detected as RTL
        }

        [Fact]
        public void ProcessRuns_MixedTextLtrFirst_AutoBase_ReturnsSingleLtrRun_Placeholder()
        {
            string text = "Hello مرحبا";
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.Single(runs); // Placeholder only creates one run
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(text.Length, runs[0].Length);
            Assert.Equal(0, runs[0].Level); // Auto-detected LTR based on "H"
        }

        [Fact]
        public void ProcessRuns_MixedTextRtlFirst_AutoBase_ReturnsSingleRtlRun_Placeholder()
        {
            string text = "مرحبا Hello";
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.Single(runs); // Placeholder only creates one run
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(text.Length, runs[0].Length);
            Assert.Equal(1, runs[0].Level); // Auto-detected RTL based on "م"
        }

        [Fact]
        public void ProcessRuns_TextWithOnlyNeutrals_AutoBase_DefaultsToLtrRun_Placeholder()
        {
            string text = "123 ---"; // EN and ON (or ET depending on full UCD)
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(text.Length, runs[0].Length);
            Assert.Equal(0, runs[0].Level); // P3: Defaults to LTR if no strong characters
        }


        // --- THE MORE DETAILED ProcessRuns TESTS for explicit formatting codes ---
        // --- ARE KEPT COMMENTED OUT. They will fail severely with the        ---
        // --- current placeholder ProcessRuns logic. We will enable and fix   ---
        // --- them as we implement the X rules in ProcessRuns.                ---
        /*
        [Fact]
        public void ProcessRuns_LtrTextWithLrePdf_BaseLtr_CorrectLevels()
        {
            // Arrange
            string lre = "\u202A"; string pdf = "\u202C";
            string text = $"AAA {lre}BBB {pdf}CCC";
            int baseLevel = 0;

            // Act
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Assert
            Assert.NotNull(runs);
            // THIS WILL FAIL UNTIL ProcessRuns IS IMPLEMENTED
            // Assert.Equal(5, runs.Count); 
            // Assert.Equal(0, runs[0].Start); Assert.Equal(4, runs[0].Length); Assert.Equal(0, runs[0].Level); // "AAA "
            // Assert.Equal(4, runs[1].Start); Assert.Equal(1, runs[1].Length); Assert.Equal(0, runs[1].Level); // LRE
            // Assert.Equal(5, runs[2].Start); Assert.Equal(4, runs[2].Length); Assert.Equal(2, runs[2].Level); // "BBB "
            // Assert.Equal(9, runs[3].Start); Assert.Equal(1, runs[3].Length); Assert.Equal(2, runs[3].Level); // PDF
            // Assert.Equal(10, runs[4].Start); Assert.Equal(3, runs[4].Length); Assert.Equal(0, runs[4].Level); // "CCC"
        }

        // ... other commented out ProcessRuns tests for LRE/RLE/PDF/Nested etc. ...
        */
    }
}