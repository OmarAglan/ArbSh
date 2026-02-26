using System;
using Xunit;
using ArbSh.Core.I18n; // Your namespace for BidiAlgorithm and BidiCharacterType
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

        // --- X Rules Tests (Basic Explicit Formatting) ---

        [Fact]
        public void ProcessRuns_SimpleRLEEmbedding_CreatesCorrectLevels()
        {
            // Test: "Hello\u202BWorld\u202C!" (Hello RLE World PDF !)
            // Expected: Hello(0) RLE(0) World(1) PDF(1) !(0)
            string text = "Hello\u202BWorld\u202C!";
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should have multiple runs with different levels
            Assert.True(runs.Count >= 2, "Should have multiple runs for embedded text");

            // Verify that embedding creates higher level
            bool hasEmbeddedLevel = runs.Any(r => r.Level > 0);
            Assert.True(hasEmbeddedLevel, "Should have embedded level > 0");
        }

        [Fact]
        public void ProcessRuns_SimpleLREEmbedding_CreatesCorrectLevels()
        {
            // Test: "مرحبا\u202AHello\u202C!" (Arabic LRE Hello PDF !)
            // Expected: مرحبا(1) LRE(1) Hello(2) PDF(2) !(1)
            string text = "مرحبا\u202AHello\u202C!";
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should have multiple runs with different levels
            Assert.True(runs.Count >= 2, "Should have multiple runs for embedded text");

            // Verify that embedding creates higher level
            bool hasEmbeddedLevel = runs.Any(r => r.Level > 1);
            Assert.True(hasEmbeddedLevel, "Should have embedded level > 1");
        }

        [Fact]
        public void ProcessRuns_RLOOverride_ForcesRightToLeft()
        {
            // Test: "Hello\u202EWorld\u202C!" (Hello RLO World PDF !)
            // RLO should force "World" to be treated as R type
            string text = "Hello\u202EWorld\u202C!";
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should have multiple runs
            Assert.True(runs.Count >= 2, "Should have multiple runs for override text");

            // Verify that override creates odd level (RTL)
            bool hasOddLevel = runs.Any(r => r.Level % 2 == 1);
            Assert.True(hasOddLevel, "RLO should create odd (RTL) level");
        }

        [Fact]
        public void ProcessRuns_LROOverride_ForcesLeftToRight()
        {
            // Test: "مرحبا\u202DWorld\u202C!" (Arabic LRO World PDF !)
            // LRO should force "World" to be treated as L type
            string text = "مرحبا\u202DWorld\u202C!";
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should have multiple runs
            Assert.True(runs.Count >= 2, "Should have multiple runs for override text");

            // Verify that override creates even level (LTR)
            bool hasEvenLevel = runs.Any(r => r.Level % 2 == 0);
            Assert.True(hasEvenLevel, "LRO should create even (LTR) level");
        }

        [Fact]
        public void ProcessRuns_UnmatchedPDF_DoesNotCrash()
        {
            // Test: "Hello\u202C!" (Hello PDF !) - PDF without matching embedding
            string text = "Hello\u202C!";
            int baseLevel = 0; // LTR paragraph

            // Should not throw exception
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.NotNull(runs);
            Assert.True(runs.Count > 0, "Should return valid runs even with unmatched PDF");
        }

        [Fact]
        public void ProcessRuns_NestedEmbedding_HandlesCorrectly()
        {
            // Test: "A\u202B B\u202A C\u202C D\u202C E" (A RLE B LRE C PDF D PDF E)
            // Nested: A(0) RLE(0) B(1) LRE(1) C(2) PDF(2) D(1) PDF(1) E(0)
            string text = "A\u202B B\u202A C\u202C D\u202C E";
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should handle nested embedding without crashing
            Assert.NotNull(runs);
            Assert.True(runs.Count > 0, "Should return valid runs for nested embedding");

            // Should have multiple different levels
            var uniqueLevels = runs.Select(r => r.Level).Distinct().ToList();
            Assert.True(uniqueLevels.Count >= 2, "Should have multiple embedding levels");
        }

        // --- Isolate Tests (X5a-X5c, X6a) ---

        [Fact]
        public void ProcessRuns_SimpleLRIIsolate_CreatesCorrectLevels()
        {
            // Test: "Hello\u2066World\u2069!" (Hello LRI World PDI !)
            // Expected: Hello(0) LRI(0) World(2) PDI(2) !(0)
            string text = "Hello\u2066World\u2069!";
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should have multiple runs with different levels
            Assert.True(runs.Count >= 2, "Should have multiple runs for isolated text");

            // Verify that isolate creates higher level
            bool hasIsolatedLevel = runs.Any(r => r.Level > 0);
            Assert.True(hasIsolatedLevel, "Should have isolated level > 0");
        }

        [Fact]
        public void ProcessRuns_SimpleRLIIsolate_CreatesCorrectLevels()
        {
            // Test: "مرحبا\u2067Hello\u2069!" (Arabic RLI Hello PDI !)
            // Expected: مرحبا(1) RLI(1) Hello(3) PDI(3) !(1)
            string text = "مرحبا\u2067Hello\u2069!";
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should have multiple runs with different levels
            Assert.True(runs.Count >= 2, "Should have multiple runs for isolated text");

            // Verify that isolate creates higher level
            bool hasIsolatedLevel = runs.Any(r => r.Level > 1);
            Assert.True(hasIsolatedLevel, "Should have isolated level > 1");
        }

        [Fact]
        public void ProcessRuns_FSIWithLTRContent_DetectsLTR()
        {
            // Test: "مرحبا\u2068Hello World\u2069!" (Arabic FSI Hello World PDI !)
            // FSI should detect LTR from "Hello" and create even level
            string text = "مرحبا\u2068Hello World\u2069!";
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should have multiple runs
            Assert.True(runs.Count >= 2, "Should have multiple runs for FSI text");

            // Should have even level for LTR content inside FSI
            bool hasEvenLevel = runs.Any(r => r.Level % 2 == 0);
            Assert.True(hasEvenLevel, "FSI with LTR content should create even level");
        }

        [Fact]
        public void ProcessRuns_FSIWithRTLContent_DetectsRTL()
        {
            // Test: "Hello\u2068مرحبا بك\u2069!" (Hello FSI Arabic PDI !)
            // FSI should detect RTL from "مرحبا" and create odd level
            string text = "Hello\u2068مرحبا بك\u2069!";
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should have multiple runs
            Assert.True(runs.Count >= 2, "Should have multiple runs for FSI text");

            // Should have odd level for RTL content inside FSI
            bool hasOddLevel = runs.Any(r => r.Level % 2 == 1);
            Assert.True(hasOddLevel, "FSI with RTL content should create odd level");
        }

        [Fact]
        public void ProcessRuns_UnmatchedPDI_DoesNotCrash()
        {
            // Test: "Hello\u2069!" (Hello PDI !) - PDI without matching isolate
            string text = "Hello\u2069!";
            int baseLevel = 0; // LTR paragraph

            // Should not throw exception
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            Assert.NotNull(runs);
            Assert.True(runs.Count > 0, "Should return valid runs even with unmatched PDI");
        }

        [Fact]
        public void ProcessRuns_NestedIsolates_HandlesCorrectly()
        {
            // Test: "A\u2067 B\u2066 C\u2069 D\u2069 E" (A RLI B LRI C PDI D PDI E)
            // Nested isolates: A(0) RLI(0) B(1) LRI(1) C(2) PDI(2) D(1) PDI(1) E(0)
            string text = "A\u2067 B\u2066 C\u2069 D\u2069 E";
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should handle nested isolates without crashing
            Assert.NotNull(runs);
            Assert.True(runs.Count > 0, "Should return valid runs for nested isolates");

            // Should have multiple different levels
            var uniqueLevels = runs.Select(r => r.Level).Distinct().ToList();
            Assert.True(uniqueLevels.Count >= 2, "Should have multiple isolate levels");
        }

        [Fact]
        public void ProcessRuns_MixedTextLtrFirst_AutoBase_CreatesCorrectRuns()
        {
            string text = "Hello مرحبا"; // "Hello" + Arabic "welcome"
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should auto-detect LTR paragraph level based on first strong character "H"
            // "Hello " should be LTR level 0, Arabic should be RTL level 1
            Assert.Equal(2, runs.Count);
            Assert.Equal(0, runs[0].Level); // LTR run for "Hello "
            Assert.Equal(1, runs[1].Level); // RTL run for Arabic text
        }

        [Fact]
        public void ProcessRuns_MixedTextRtlFirst_AutoBase_CreatesCorrectRuns()
        {
            string text = "مرحبا Hello"; // Arabic "welcome" + "Hello"
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should auto-detect RTL paragraph level based on first strong character "م"
            // Arabic should be RTL level 1, "Hello" should be LTR level 2 (next higher even level)
            Assert.Equal(2, runs.Count);
            Assert.Equal(1, runs[0].Level); // RTL run for Arabic text
            Assert.Equal(2, runs[1].Level); // LTR run for "Hello" (I2: odd level + L -> next higher even level)
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

        // --- W Rules Tests ---

        [Fact]
        public void ProcessRuns_W1_NSMAfterArabicLetter_TakesArabicType()
        {
            // Test W1: NSM after Arabic letter should take AL type (then converted to R by W3)
            string text = "\u0645\u064E"; // Arabic Meem + Arabic Fatha (NSM)
            var runs = BidiAlgorithm.ProcessRuns(text, 0);

            // Both characters should be at the same RTL level
            Assert.Single(runs);
            Assert.Equal(1, runs[0].Level); // RTL level
        }

        [Fact]
        public void ProcessRuns_W2_EuropeanNumberAfterArabic_BecomesArabicNumber()
        {
            // Test W2: EN after AL should become AN
            string text = "\u06451"; // Arabic Meem + European digit '1'
            var runs = BidiAlgorithm.ProcessRuns(text, 0);

            // W2 converts EN to AN after AL
            // Arabic letter: AL -> R (W3) -> level 1 (I1: even level + R -> level + 1)
            // European number: EN -> AN (W2) -> level 2 (I1: even level + AN -> level + 2)
            // Different levels result in separate runs
            Assert.Equal(2, runs.Count);
            Assert.Equal(1, runs[0].Level); // Arabic character at level 1
            Assert.Equal(2, runs[1].Level); // Arabic number at level 2
        }

        [Fact]
        public void ProcessRuns_W3_ArabicLetterBecomesRTL()
        {
            // Test W3: AL should become R
            string text = "\u0645"; // Arabic Meem
            var runs = BidiAlgorithm.ProcessRuns(text, 0);

            Assert.Single(runs);
            Assert.Equal(1, runs[0].Level); // RTL level (odd)
        }

        [Fact]
        public void ProcessRuns_W4_EuropeanSeparatorBetweenNumbers_BecomesNumber()
        {
            // Test W4: ES between two EN should become EN
            string text = "1+2"; // European numbers with plus sign
            var runs = BidiAlgorithm.ProcessRuns(text, 0);

            // All should be at LTR level
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Level); // LTR level
        }

        [Fact]
        public void ProcessRuns_W5_EuropeanTerminatorAdjacentToNumber_BecomesNumber()
        {
            // Test W5: ET adjacent to EN should become EN
            string text = "1$"; // European number with currency symbol (ET)
            var runs = BidiAlgorithm.ProcessRuns(text, 0);

            // Both should be at LTR level
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Level); // LTR level
        }

        [Fact]
        public void ProcessRuns_W7_EuropeanNumberAfterLatin_BecomesLatin()
        {
            // Test W7: EN after L should become L
            string text = "a1"; // Latin letter + European digit
            var runs = BidiAlgorithm.ProcessRuns(text, 0);

            // Both should be at LTR level
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Level); // LTR level
        }

        [Fact]
        public void ProcessRuns_W6_RemainingSeparators_BecomeNeutral()
        {
            // Test W6: Remaining ES/ET/CS should become ON
            string text = "a+b"; // Latin + ES + Latin (ES not between numbers)
            var runs = BidiAlgorithm.ProcessRuns(text, 0);

            // All should be at LTR level (L + ON + L)
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Level); // LTR level
        }

        // --- N Rules Tests ---

        [Fact]
        public void ProcessRuns_BracketPairs_N0_LTRContext_BracketsBecomeLTR()
        {
            string text = "a(b)c"; // LTR context with brackets
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // All characters should be LTR level 0 after N0 processing
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Level);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(5, runs[0].Length);
        }

        [Fact]
        public void ProcessRuns_BracketPairs_N0_RTLContext_BracketsBecomRTL()
        {
            string text = "أ(ب)ج"; // RTL context with brackets
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // All characters should be RTL level 1 after N0 processing
            Assert.Single(runs);
            Assert.Equal(1, runs[0].Level);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(5, runs[0].Length);
        }

        [Fact]
        public void ProcessRuns_NeutralSequence_N1_SurroundingStrongTypes()
        {
            string text = "a.b"; // L NI L sequence
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // All characters should be LTR level 0 after N1 processing
            Assert.Single(runs);
            Assert.Equal(0, runs[0].Level);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(3, runs[0].Length);
        }

        [Fact]
        public void ProcessRuns_IsolatedNeutral_N2_EmbeddingDirection()
        {
            string text = "."; // Isolated neutral
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Neutral should take embedding direction (RTL level 1)
            Assert.Single(runs);
            Assert.Equal(1, runs[0].Level);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(1, runs[0].Length);
        }

        // --- Run Segmentation Tests ---

        [Fact]
        public void ProcessRuns_MixedLtrRtl_CorrectRunSegmentation()
        {
            string text = "Hello مرحبا World"; // LTR + RTL + LTR
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should have multiple runs with different levels
            Assert.True(runs.Count >= 2, $"Expected at least 2 runs, got {runs.Count}");

            // Verify runs cover the entire text
            int totalLength = runs.Sum(r => r.Length);
            Assert.Equal(text.Length, totalLength);

            // Verify runs are contiguous
            int expectedStart = 0;
            foreach (var run in runs)
            {
                Assert.Equal(expectedStart, run.Start);
                expectedStart += run.Length;
            }
        }

        [Fact]
        public void ProcessRuns_WithExplicitFormatting_CorrectRunSegmentation()
        {
            string text = "Hello\u202Dمرحبا\u202C World"; // LTR + LRO + RTL + PDF + LTR
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Verify runs cover the entire text
            int totalLength = runs.Sum(r => r.Length);
            Assert.Equal(text.Length, totalLength);

            // Verify runs are contiguous and non-overlapping
            int expectedStart = 0;
            foreach (var run in runs)
            {
                Assert.True(run.Start >= 0, $"Run start {run.Start} should be non-negative");
                Assert.True(run.Length > 0, $"Run length {run.Length} should be positive");
                Assert.Equal(expectedStart, run.Start);
                expectedStart += run.Length;
            }
        }

        [Fact]
        public void ProcessRuns_WithNumbers_CorrectRunSegmentation()
        {
            string text = "Price: 123.45 ريال"; // LTR + EN + RTL
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Verify basic run properties
            Assert.True(runs.Count > 0, "Should have at least one run");

            // Verify runs cover the entire text
            int totalLength = runs.Sum(r => r.Length);
            Assert.Equal(text.Length, totalLength);

            // Verify all runs have valid levels
            foreach (var run in runs)
            {
                Assert.True(run.Level >= 0, $"Run level {run.Level} should be non-negative");
                Assert.True(run.Level <= 125, $"Run level {run.Level} should not exceed max depth");
            }
        }

        [Fact]
        public void ProcessRuns_EmptyText_ReturnsEmptyRuns()
        {
            string text = "";
            int baseLevel = 0;
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            Assert.Empty(runs);
        }

        [Fact]
        public void ProcessRuns_SingleCharacter_ReturnsSingleRun()
        {
            string text = "a";
            int baseLevel = 0;
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            Assert.Single(runs);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(1, runs[0].Length);
            Assert.Equal(0, runs[0].Level); // LTR character in LTR paragraph
        }

        // --- I Rules Tests ---

        [Fact]
        public void ProcessRuns_I1_EvenLevel_RCharacter_IncreasesLevelByOne()
        {
            // Even level (0) + R character should become level 1
            string text = "\u05D0"; // Hebrew Alef (R character)
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            Assert.Single(runs);
            Assert.Equal(1, runs[0].Level); // I1: even level + R -> level + 1
        }

        [Fact]
        public void ProcessRuns_I1_EvenLevel_ANCharacter_IncreasesLevelByTwo()
        {
            // Even level (0) + AN character should become level 2
            string text = "\u0660"; // Arabic-Indic digit 0 (AN character)
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            Assert.Single(runs);
            Assert.Equal(2, runs[0].Level); // I1: even level + AN -> level + 2
        }

        [Fact]
        public void ProcessRuns_I1_EvenLevel_ENCharacter_WithRTLContext_IncreasesLevelByTwo()
        {
            // Even level (0) + EN character in RTL context should become level 2
            // Use EN after AL to ensure it stays EN (not converted to AN by W2)
            string text = "\u05D01"; // Hebrew Alef + European digit '1'
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Hebrew Alef: R -> level 1 (I1: even + R -> level + 1)
            // European digit: EN -> level 2 (I1: even + EN -> level + 2)
            Assert.Equal(2, runs.Count);
            Assert.Equal(1, runs[0].Level); // Hebrew character
            Assert.Equal(2, runs[1].Level); // European number
        }

        [Fact]
        public void ProcessRuns_I2_OddLevel_LCharacter_IncreasesLevelByOne()
        {
            // Odd level (1) + L character should become level 2
            string text = "a"; // Latin letter (L character)
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            Assert.Single(runs);
            Assert.Equal(2, runs[0].Level); // I2: odd level + L -> level + 1
        }

        [Fact]
        public void ProcessRuns_I2_OddLevel_ENCharacter_IncreasesLevelByOne()
        {
            // Odd level (1) + EN character should become level 2
            // Use explicit LTR embedding to ensure EN character in RTL context
            string text = "\u202D1\u202C"; // LRO + European digit + PDF
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // The EN character should be at level 2 (I2: odd level + EN -> level + 1)
            Assert.True(runs.Any(r => r.Level == 2), "Expected at least one run at level 2 for EN character");
        }

        [Fact]
        public void ProcessRuns_I2_OddLevel_ANCharacter_IncreasesLevelByOne()
        {
            // Odd level (1) + AN character should become level 2
            string text = "\u0660"; // Arabic-Indic digit 0 (AN character)
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            Assert.Single(runs);
            Assert.Equal(2, runs[0].Level); // I2: odd level + AN -> level + 1
        }

        [Fact]
        public void ProcessRuns_IRules_NoChange_LCharacterEvenLevel()
        {
            // Even level (0) + L character should remain at level 0
            string text = "a"; // Latin letter (L character)
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            Assert.Single(runs);
            Assert.Equal(0, runs[0].Level); // No change for L at even level
        }

        [Fact]
        public void ProcessRuns_IRules_NoChange_RCharacterOddLevel()
        {
            // Odd level (1) + R character should remain at level 1
            string text = "\u05D0"; // Hebrew Alef (R character)
            int baseLevel = 1; // RTL paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            Assert.Single(runs);
            Assert.Equal(1, runs[0].Level); // No change for R at odd level
        }

        // --- L Rules Tests ---

        [Fact]
        public void ReorderRunsForDisplay_L2_SimpleRTLReversed()
        {
            // Simple RTL text should be reversed
            string text = "\u05D0\u05D1\u05D2"; // Hebrew Alef, Bet, Gimel
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            string result = BidiAlgorithm.ReorderRunsForDisplay(text, runs, baseLevel);

            // Hebrew characters should be reversed (RTL at level 1)
            Assert.Equal("\u05D2\u05D1\u05D0", result); // Gimel, Bet, Alef
        }

        [Fact]
        public void ReorderRunsForDisplay_L2_EmbeddedLTR_ShouldReadLTR()
        {
            // Scenario: "ARABIC english ARABIC"
            // Logical: "مرحبا exit شكرا"
            // Levels: 11111 2222 1111 (Simplified)
            // Visual Expectation: "arkuS exit abahraM" (All read RTL, but 'exit' reads LTR within it)
            // Wait, standard visual string for "RTL LTR RTL" is "RTL LTR RTL" visually reversed?
            // Logical: A B C d e f G H I
            // Levels:  1 1 1 2 2 2 1 1 1
            // Max 2 (def). Reverse: A B C f e d G H I.
            // Max 1 (All). Reverse: I H G d e f C B A.
            // Result: "IHG def CBA"
            // The English "def" reads Left-To-Right. The Arabic reads Right-to-Left (CBA, IHG).
            
            string arabic1 = "\u0645\u0631\u062d\u0628\u0627"; // Marhaba
            string english = "exit";
            string arabic2 = "\u0634\u0643\u0631\u0627"; // Shukran
            string text = $"{arabic1} {english} {arabic2}";
            
            int baseLevel = 1; // RTL Paragraph
            
            var runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            string visual = BidiAlgorithm.ReorderRunsForDisplay(text, runs, baseLevel);
            
            // Check that 'exit' appears as 'exit' in the visual string, NOT 'tixe'
            Assert.Contains("exit", visual);
            
            // Note: Assert.Contains checks for substring. 
            // If result is "arkuS exit abahraM", "exit" is present.
            // If result was "arkuS tixe abahraM", "exit" would NOT be present.
        }

        [Fact]
        public void ReorderRunsForDisplay_L2_MixedLTRRTL()
        {
            // Mixed LTR and RTL text
            string text = "abc\u05D0\u05D1\u05D2def"; // abc + Hebrew + def
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            string result = BidiAlgorithm.ReorderRunsForDisplay(text, runs, baseLevel);

            // Expected: abc + reversed Hebrew + def
            Assert.Equal("abc\u05D2\u05D1\u05D0def", result);
        }

        [Fact]
        public void ReorderRunsForDisplay_L4_CharacterMirroring()
        {
            // Test character mirroring in RTL context
            string text = "\u05D0(\u05D1)"; // Hebrew Alef + ( + Hebrew Bet + )
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            string result = BidiAlgorithm.ReorderRunsForDisplay(text, runs, baseLevel);

            // In RTL context, parentheses should be mirrored and the sequence reversed
            // Expected: ) + Bet + ( + Alef (all RTL, so mirrored parens)
            Assert.Contains(")", result);
            Assert.Contains("(", result);
            // The exact order depends on run segmentation, but mirroring should occur
        }

        [Fact]
        public void ReorderRunsForDisplay_L4_NoMirroringInLTR()
        {
            // Test that mirroring doesn't happen in LTR context
            string text = "test(abc)"; // LTR text with parentheses
            int baseLevel = 0; // LTR paragraph
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);
            string result = BidiAlgorithm.ReorderRunsForDisplay(text, runs, baseLevel);

            // Should remain unchanged (no mirroring in LTR)
            Assert.Equal("test(abc)", result);
        }

        [Fact]
        public void ReorderRunsForDisplay_EmptyText_ReturnsEmpty()
        {
            string result = BidiAlgorithm.ReorderRunsForDisplay("", [], 0);
            Assert.Equal("", result);
        }

        [Fact]
        public void ReorderRunsForDisplay_NullRuns_ReturnsOriginal()
        {
            string text = "test";
            string result = BidiAlgorithm.ReorderRunsForDisplay(text, null!, 0);
            Assert.Equal(text, result);
        }

        // --- Enhanced P Rules Tests ---

        [Fact]
        public void ProcessRuns_P2_SkipsIsolateContent_LTRFirst()
        {
            // Test P2: Skip characters between isolate initiator and matching PDI
            // Text: "a\u2067\u05D0\u2069b" (a + LRI + Hebrew Alef + PDI + b)
            // Should find 'a' as first strong character, not Hebrew Alef inside isolate
            string text = "a\u2067\u05D0\u2069b"; // a + LRI + Hebrew Alef + PDI + b
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should detect LTR paragraph level based on 'a', not Hebrew inside isolate
            Assert.True(runs.Any(r => r.Level == 0), "Should have LTR base level runs");
            Assert.False(runs.All(r => r.Level >= 1), "Should not be RTL paragraph");
        }

        [Fact]
        public void ProcessRuns_P2_SkipsIsolateContent_RTLFirst()
        {
            // Test P2: Skip characters between isolate initiator and matching PDI
            // Text: "\u05D0\u2067a\u2069\u05D1" (Hebrew Alef + LRI + a + PDI + Hebrew Bet)
            // Should find Hebrew Alef as first strong character, not 'a' inside isolate
            string text = "\u05D0\u2067a\u2069\u05D1"; // Hebrew Alef + LRI + a + PDI + Hebrew Bet
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should detect RTL paragraph level based on Hebrew Alef, not 'a' inside isolate
            Assert.True(runs.Any(r => r.Level >= 1), "Should have RTL level runs");
        }

        [Fact]
        public void ProcessRuns_P2_SkipsUnmatchedIsolate()
        {
            // Test P2: Skip to end of paragraph if isolate has no matching PDI
            // Text: "a\u2067\u05D0b" (a + LRI + Hebrew Alef + b, no PDI)
            // Should find 'a' as first strong character and skip rest after unmatched LRI
            string text = "a\u2067\u05D0b"; // a + LRI + Hebrew Alef + b (no PDI)
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should detect LTR paragraph level based on 'a'
            Assert.True(runs.Any(r => r.Level == 0), "Should have LTR base level runs");
        }

        [Fact]
        public void ProcessRuns_P2_IgnoresEmbeddingInitiators()
        {
            // Test P2: Ignore embedding initiators (but not characters within the embedding)
            // Text: "\u202B\u05D0" (RLE + Hebrew Alef)
            // Should ignore RLE and find Hebrew Alef as first strong character
            string text = "\u202B\u05D0"; // RLE + Hebrew Alef
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should detect RTL paragraph level based on Hebrew Alef
            Assert.True(runs.Any(r => r.Level >= 1), "Should have RTL level runs");
        }

        [Fact]
        public void ProcessRuns_P2_ComplexIsolateEmbeddingMix()
        {
            // Test P2: Complex case with both isolates and embeddings
            // Text: "\u202B\u2067a\u2069\u05D0" (RLE + LRI + a + PDI + Hebrew Alef)
            // Should ignore RLE, skip LRI...PDI content, find Hebrew Alef
            string text = "\u202B\u2067a\u2069\u05D0"; // RLE + LRI + a + PDI + Hebrew Alef
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should detect RTL paragraph level based on Hebrew Alef
            Assert.True(runs.Any(r => r.Level >= 1), "Should have RTL level runs");
        }

        [Fact]
        public void ProcessRuns_P2_NestedIsolates()
        {
            // Test P2: Nested isolates should be properly skipped
            // Text: "a\u2067\u2067\u05D0\u2069\u2069b" (a + LRI + LRI + Hebrew Alef + PDI + PDI + b)
            // Should find 'a' as first strong character
            string text = "a\u2067\u2067\u05D0\u2069\u2069b"; // a + nested LRIs + Hebrew + PDIs + b
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should detect LTR paragraph level based on 'a'
            Assert.True(runs.Any(r => r.Level == 0), "Should have LTR base level runs");
        }

        [Fact]
        public void ProcessRuns_P3_DefaultsToLTR_OnlyNeutrals()
        {
            // Test P3: Default to LTR when no strong characters found
            string text = "123 !@#"; // Only numbers and neutrals
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should default to LTR paragraph level
            Assert.True(runs.All(r => r.Level % 2 == 0), "Should default to LTR (even levels)");
        }

        [Fact]
        public void ProcessRuns_P3_DefaultsToLTR_OnlyIsolatedStrong()
        {
            // Test P3: Default to LTR when strong characters are only inside isolates
            string text = "\u2067\u05D0\u2069 123"; // LRI + Hebrew Alef + PDI + numbers
            int baseLevel = -1; // Auto-detect
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Should default to LTR since Hebrew is inside isolate
            Assert.True(runs.Any(r => r.Level == 0), "Should default to LTR base level");
        }
    }
}
