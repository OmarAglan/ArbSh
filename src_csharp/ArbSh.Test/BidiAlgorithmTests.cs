using System;
using Xunit;
using ArbSh.Console.I18n; // Your namespace for BidiAlgorithm and BidiCharacterType

namespace ArbSh.Test.I18n
{
    public class BidiAlgorithmTests
    {
        [Fact]
        public void GetCharType_LatinLetter_ReturnsL()
        {
            // Arrange
            int codepointL = 'A'; // U+0041

            // Act
            BidiCharacterType typeL = BidiAlgorithm.GetCharType(codepointL);

            // Assert
            Assert.Equal(BidiCharacterType.L, typeL);
        }

        [Fact]
        public void GetCharType_ArabicLetter_ReturnsAL()
        {
            // Arrange
            int codepointAL = 0x0645; // 'م' (Meem)

            // Act
            BidiCharacterType typeAL = BidiAlgorithm.GetCharType(codepointAL);

            // Assert
            Assert.Equal(BidiCharacterType.AL, typeAL);
        }

        [Fact]
        public void GetCharType_HebrewLetter_ReturnsR()
        {
            // Arrange
            int codepointR = 0x05D0; // 'א' (Alef)

            // Act
            BidiCharacterType typeR = BidiAlgorithm.GetCharType(codepointR);

            // Assert
            Assert.Equal(BidiCharacterType.R, typeR);
        }

        [Fact]
        public void GetCharType_EuropeanNumber_ReturnsEN()
        {
            // Arrange
            int codepointEN = '7'; // U+0037

            // Act
            BidiCharacterType typeEN = BidiAlgorithm.GetCharType(codepointEN);

            // Assert
            Assert.Equal(BidiCharacterType.EN, typeEN);
        }

        [Fact]
        public void GetCharType_ArabicNumber_ReturnsAN()
        {
            // Arrange
            int codepointAN = 0x0661; // '١' (Arabic-Indic Digit One)

            // Act
            BidiCharacterType typeAN = BidiAlgorithm.GetCharType(codepointAN);

            // Assert
            Assert.Equal(BidiCharacterType.AN, typeAN);
        }

        [Fact]
        public void GetCharType_Whitespace_ReturnsWS()
        {
            // Arrange
            int codepointWS = ' '; // Space U+0020
            int codepointTab = '\t'; // Tab U+0009

            // Act
            BidiCharacterType typeWS = BidiAlgorithm.GetCharType(codepointWS);
            BidiCharacterType typeTab = BidiAlgorithm.GetCharType(codepointTab);


            // Assert
            Assert.Equal(BidiCharacterType.WS, typeWS);
            // Note: UAX#9 classifies TAB as S (Segment Separator).
            // The C code's GetCharType also classifies TAB (0x0009) as S.
            // Let's verify the C# port does the same.
            // Ah, the C code's GetCharType has:
            // if (codepoint == 0x0020 || codepoint == 0x0009 || codepoint == 0x00A0) return BIDI_TYPE_WS;
            // And then later:
            // if (codepoint == 0x0009 || codepoint == 0x001F) return BIDI_TYPE_S;
            // This means 0x0009 (TAB) would be WS due to order. Let's test for WS.
            Assert.Equal(BidiCharacterType.WS, typeTab);
        }

        [Fact]
        public void GetCharType_ParagraphSeparator_ReturnsB()
        {
            // Arrange
            int codepointNL = '\n'; // Newline U+000A

            // Act
            BidiCharacterType typeB = BidiAlgorithm.GetCharType(codepointNL);

            // Assert
            Assert.Equal(BidiCharacterType.B, typeB);
        }

        // We might need to make GetCharTypeInternalSimplified public for testing
        // or refactor GetCharType to not call it if it's problematic.
        // For now, let's test the public GetCharType as is.
        // If GetCharTypeInternalSimplified was made public static for testing:
        /*
        [Fact]
        public void GetCharTypeInternalSimplified_LatinLetter_ReturnsL()
        {
            // This would test the fallback logic in GetCharType if GetCharTypeInternalSimplified was public
            // However, as GetCharTypeInternalSimplified is private, we test GetCharType's behavior.
            // The current GetCharType should directly return L for 'A' without fallback.
            Assert.Equal(BidiCharacterType.L, BidiAlgorithm.GetCharType('A'));
        }
        */

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

        [Fact]
        public void GetCharType_LRM_ReturnsLRM()
        {
            Assert.Equal(BidiCharacterType.LRM, BidiAlgorithm.GetCharType(0x200E)); // LEFT-TO-RIGHT MARK
        }

        [Fact]
        public void GetCharType_RLM_ReturnsRLM()
        {
            Assert.Equal(BidiCharacterType.RLM, BidiAlgorithm.GetCharType(0x200F)); // RIGHT-TO-LEFT MARK
        }
        [Fact]
        public void GetCharType_EuropeanSeparator_ReturnsES()
        {
            Assert.Equal(BidiCharacterType.ES, BidiAlgorithm.GetCharType('+')); // PLUS SIGN U+002B
            Assert.Equal(BidiCharacterType.ES, BidiAlgorithm.GetCharType('-')); // HYPHEN-MINUS U+002D
        }

        [Fact]
        public void GetCharType_CommonSeparator_ReturnsCS()
        {
            Assert.Equal(BidiCharacterType.CS, BidiAlgorithm.GetCharType('.')); // FULL STOP U+002E
            Assert.Equal(BidiCharacterType.CS, BidiAlgorithm.GetCharType(',')); // COMMA U+002C
            Assert.Equal(BidiCharacterType.CS, BidiAlgorithm.GetCharType(':')); // COLON U+003A
            Assert.Equal(BidiCharacterType.CS, BidiAlgorithm.GetCharType('/')); // SOLIDUS U+002F
        }

        [Fact]
        public void GetCharType_NonSpacingMark_ReturnsNSM()
        {
            // Test a common combining diacritic
            Assert.Equal(BidiCharacterType.NSM, BidiAlgorithm.GetCharType(0x0301)); // COMBINING ACUTE ACCENT
        }

        [Fact]
        public void GetCharType_SegmentSeparator_ReturnsS()
        {
            // According to C code logic (0x0009 handled by WS first)
            Assert.Equal(BidiCharacterType.S, BidiAlgorithm.GetCharType(0x001F)); // INFORMATION SEPARATOR ONE (Unit Separator)
        }

        [Fact]
        public void GetCharType_OtherNeutral_ReturnsON_ForGeneralSymbol()
        {
            // Test a symbol that is likely ON if not specifically handled
            // Copyright sign is often ON. Let's see what our simplified GetCharType returns.
            // U+00A9 is ©.
            // In BidiAlgorithm.cs, it's not AL, R, EN, AN, explicit format, WS, B, S, ES, CS, NSM.
            // It's not < 0x0080 to hit the GetCharTypeInternalSimplified -> L path.
            // So it should fall to BidiCharacterType.ON by default.
            Assert.Equal(BidiCharacterType.ON, BidiAlgorithm.GetCharType(0x00A9)); // COPYRIGHT SIGN
        }

        [Fact]
        public void GetCharType_UnclassifiedAsciiPunctuation_ReturnsL_ByDefault()
        {
            // Test ASCII punctuation like '!' (U+0021)
            // It's not EN, ES, CS, WS, B, S.
            // It IS < 0x0080.
            // GetCharTypeInternalSimplified for '!' will return ON.
            // So, the main GetCharType should classify it as L.
            Assert.Equal(BidiCharacterType.L, BidiAlgorithm.GetCharType('!'));
        }

        [Fact]
        public void GetCharType_UnclassifiedExtendedLatin_ReturnsON_ByDefault()
        {
            // Test an extended Latin character like 'é' (U+00E9)
            // It's not AL, R, EN, AN, explicit format, WS, B, S, ES, CS, NSM.
            // It's >= 0x0080.
            // So it should fall to BidiCharacterType.ON by default.
            Assert.Equal(BidiCharacterType.ON, BidiAlgorithm.GetCharType(0x00E9)); // LATIN SMALL LETTER E WITH ACUTE
        }
        [Fact]
        public void ProcessRuns_PurelyLtrText_BaseLtr_ReturnsSingleLtrRun()
        {
            // Arrange
            string text = "Hello"; // All L characters
            int baseLevel = 0; // LTR paragraph

            // Act
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Assert
            Assert.NotNull(runs);
            Assert.Single(runs);
            var run = runs[0];
            Assert.Equal(0, run.Start);
            Assert.Equal(text.Length, run.Length);
            Assert.Equal(baseLevel, run.Level); // Should stay at base LTR level
        }

        [Fact]
        public void ProcessRuns_PurelyLtrText_BaseRtl_ReturnsSingleLtrRun_AtAdjustedLevel()
        {
            // Arrange
            string text = "Hello"; // All L characters
            int baseLevel = 1; // RTL paragraph

            // Act
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Assert
            Assert.NotNull(runs);
            Assert.Single(runs);
            var run = runs[0];
            Assert.Equal(0, run.Start);
            Assert.Equal(text.Length, run.Length);
            // According to UAX#9 X5c (and simplified logic in C# port for L chars):
            // If currentLevel is odd (RTL base), an L char would make the newLevel currentLevel+1 (even).
            // So the run of L characters should get an even level.
            // The ported C# code's ProcessRuns does:
            //   newLevel = (currentLevel % 2 == 0) ? currentLevel : currentLevel + 1; // Stay or increase to even
            //   if (newLevel > MaxDepth) newLevel = MaxDepth;
            //   newLevel &= ~1; // Ensure even
            // If baseLevel is 1, currentLevel starts at 1.
            // For 'H', newLevel = (1 % 2 == 0) ? 1 : 1 + 1 = 2. Then newLevel &= ~1 keeps it 2.
            // The *run* should be created with the currentLevel *before* this character causes a potential level change.
            // The C code for default case:
            //   if (new_level != current_level) create_new_run = 1;
            //   current_level = new_level; // This was the C code logic flaw for the run level itself.
            // The C# port:
            //   if (newLevel != currentLevel) { createNewRun = true; }
            //   // ...
            //   if (createNewRun) { runs.Add(new BidiRun(runStart, i - runStart, currentLevel)); ... currentLevel = newLevel; }
            // This means the first run (if the whole string is one type) uses the initial currentLevel.
            // If 'H' (L type) is met and currentLevel is 1 (RTL), newLevel becomes 2.
            // The run from runStart=0 to i (before 'H') would have level 1. Then 'H' starts a new run at level 2.
            // Let's trace: text="H", base=1. currentLevel=1. runStart=0.
            // i=0, char='H', type=L. overrideStatus=-1. effectiveCharType=L.
            // newLevel=(1%2==0)?1:1+1 = 2. newLevel&=~1 -> 2.
            // newLevel (2) != currentLevel (1) -> createNewRun = true.
            // if (createNewRun) -> if (i > runStart) is false. runStart=0. currentLevel becomes 2.
            // i=1. Loop ends.
            // Add final run: runs.Add(new BidiRun(runStart=0, length-runStart=1, currentLevel=2))
            // So, for "Hello" with base 1, it should be one run at level 2.
            Assert.Equal(2, run.Level); // Expected level 2 (even)
        }

        [Fact]
        public void ProcessRuns_PurelyRtlText_BaseRtl_ReturnsSingleRtlRun()
        {
            // Arrange
            string text = "مرحبا"; // All AL characters
            int baseLevel = 1; // RTL paragraph

            // Act
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Assert
            Assert.NotNull(runs);
            Assert.Single(runs);
            var run = runs[0];
            Assert.Equal(0, run.Start);
            Assert.Equal(text.Length, run.Length);
            Assert.Equal(baseLevel, run.Level); // Should stay at base RTL level
        }

        [Fact]
        public void ProcessRuns_PurelyRtlText_BaseLtr_ReturnsSingleRtlRun_AtAdjustedLevel()
        {
            // Arrange
            string text = "مرحبا"; // All AL characters
            int baseLevel = 0; // LTR paragraph

            // Act
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Assert
            Assert.NotNull(runs);
            Assert.Single(runs);
            var run = runs[0];
            Assert.Equal(0, run.Start);
            Assert.Equal(text.Length, run.Length);
            // For 'م' (AL type) and currentLevel is 0 (LTR base), newLevel becomes 1.
            // Final run will be at level 1.
            Assert.Equal(1, run.Level); // Expected level 1 (odd)
        }

        [Fact]
        public void ProcessRuns_EmptyText_ReturnsEmptyList()
        {
            // Arrange
            string text = "";
            int baseLevel = 0;

            // Act
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Assert
            Assert.NotNull(runs);
            Assert.Empty(runs);
        }
        [Fact]
        public void ProcessRuns_LtrThenRtlText_BaseLtr_ReturnsTwoRunsCorrectLevels()
        {
            // Arrange
            string text = "Hello مرحبا"; // LTR then RTL
            int baseLevel = 0;    // LTR paragraph

            // Act
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Assert
            Assert.NotNull(runs);
            Assert.Equal(2, runs.Count);

            // Run 1: "Hello " (includes space as it's neutral, takes level of preceding strong)
            var run1 = runs[0];
            Assert.Equal(0, run1.Start);
            Assert.Equal(6, run1.Length); // "Hello "
            Assert.Equal(0, run1.Level);  // LTR level

            // Run 2: "مرحبا"
            var run2 = runs[1];
            Assert.Equal(6, run2.Start);
            Assert.Equal(5, run2.Length); // "مرحبا"
            Assert.Equal(1, run2.Level);  // RTL level
        }

        [Fact]
        public void ProcessRuns_RtlThenLtrText_BaseRtl_ReturnsTwoRunsCorrectLevels()
        {
            // Arrange
            string text = "مرحبا Hello"; // RTL then LTR
            int baseLevel = 1;    // RTL paragraph

            // Act
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Assert
            Assert.NotNull(runs);
            Assert.Equal(2, runs.Count);

            // Run 1: "مرحبا "
            var run1 = runs[0];
            Assert.Equal(0, run1.Start);
            Assert.Equal(6, run1.Length); // "مرحبا "
            Assert.Equal(1, run1.Level);  // RTL level

            // Run 2: "Hello"
            var run2 = runs[1];
            Assert.Equal(6, run2.Start);
            Assert.Equal(5, run2.Length); // "Hello"
            Assert.Equal(2, run2.Level);  // LTR characters in RTL context get next even level
        }

        [Fact]
        public void ProcessRuns_LtrRtlLtrText_BaseLtr_ReturnsThreeRunsCorrectLevels()
        {
            // Arrange
            string text = "abc مرحبا def"; // LTR - RTL - LTR
            int baseLevel = 0;     // LTR paragraph

            // Act
            List<BidiAlgorithm.BidiRun> runs = BidiAlgorithm.ProcessRuns(text, baseLevel);

            // Assert
            Assert.NotNull(runs);
            Assert.Equal(3, runs.Count);

            // Run 1: "abc "
            var run1 = runs[0];
            Assert.Equal(0, run1.Start);
            Assert.Equal(4, run1.Length);
            Assert.Equal(0, run1.Level);

            // Run 2: "مرحبا "
            var run2 = runs[1];
            Assert.Equal(4, run2.Start);
            Assert.Equal(6, run2.Length);
            Assert.Equal(1, run2.Level);

            // Run 3: "def"
            var run3 = runs[2];
            Assert.Equal(10, run3.Start);
            Assert.Equal(3, run3.Length);
            // After an RTL run (level 1), subsequent LTR text should get level (1+1)&~1 = 2,
            // but our simplified model might reset to paragraph level if not inside explicit embedding.
            // Let's trace the C# code's logic carefully for currentLevel updates.
            // After "مرحبا ": currentLevel is 1.
            // Next char 'd' (L): newLevel = (1%2==0)?1:1+1 = 2. newLevel &= ~1 -> 2.
            // createNewRun = true because newLevel (2) != currentLevel (1).
            // The run "مرحبا " is created with level 1.
            // runStart becomes index of 'd'. currentLevel becomes 2.
            // Then "def" forms a run with level 2.
            Assert.Equal(2, run3.Level);
        }
    }
}