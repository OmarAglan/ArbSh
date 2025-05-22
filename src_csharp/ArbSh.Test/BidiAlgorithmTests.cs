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
    }
}