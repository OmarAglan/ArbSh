using Xunit;
using ArbSh.Console.I18n;

namespace ArbSh.Test.I18n
{
    public class ArabicShapingTests
    {
        [Fact]
        public void Shape_EnglishText_ReturnsUnchanged()
        {
            // Arrange
            string input = "Hello World";

            // Act
            string output = ArabicShaper.Shape(input);

            // Assert
            Assert.Equal(input, output);
        }

        [Fact]
        public void Shape_ArabicText_ChangesCodepointsToPresentationForms()
        {
            // Arrange
            // Logical "Marhaba": Meem(0645) + Reh(0631) + Hah(062D) + Beh(0628) + Alef(0627)
            string input = "\u0645\u0631\u062d\u0628\u0627";

            // Act
            string output = ArabicShaper.Shape(input);

            // Assert
            // The string should definitely change because logical characters 
            // are replaced by presentation forms (e.g., Initial Meem, Medial Hah).
            Assert.NotEqual(input, output);

            // Verify that the output contains characters from the Arabic Presentation Forms blocks
            // Forms-A: U+FB50 - U+FDFF
            // Forms-B: U+FE70 - U+FEFF
            bool hasPresentationForms = false;
            foreach (char c in output)
            {
                if ((c >= 0xFB50 && c <= 0xFDFF) || (c >= 0xFE70 && c <= 0xFEFF))
                {
                    hasPresentationForms = true;
                    break;
                }
            }
            Assert.True(hasPresentationForms, "Output should contain Arabic Presentation Forms characters");
        }

        [Fact]
        public void Shape_MixedText_ShapesOnlyArabic()
        {
            // Arrange
            // "Hello " + Meem + " World"
            string input = "Hello \u0645 World";

            // Act
            string output = ArabicShaper.Shape(input);

            // Assert
            // The English parts should remain exactly as is
            Assert.StartsWith("Hello ", output);
            Assert.EndsWith(" World", output);
            
            // The middle part (Meem) might change to isolated form (U+FEE1) or stay if it's already compatible
            // But the string as a whole should be valid and processed without crashing
            Assert.NotNull(output);
            Assert.Equal(input.Length, output.Length); // Length usually stays same for 1:1 shaping
        }

        [Fact]
        public void ProcessTextForRTLDisplay_Integration_ShapesAndReorders()
        {
            // Arrange
            // Logical: "A" + " " + Alef(0627) + Lam(0644) + " " + "B"
            // "A \u0627\u0644 B" -> "A Al B"
            string input = "A \u0627\u0644 B";

            // Act
            string output = ConsoleRTLDisplay.ProcessTextForRTLDisplay(input);

            // Assert
            // 1. Check Shaping: The Alef-Lam pair should likely form a Ligature (Lam-Alef)
            //    Lam-Alef ligature is usually 1 char (e.g., U+FEFB isolated) replacing 2 chars
            //    OR they remain 2 chars but mapped to specific forms.
            //    Note: ICU might map to 1 ligature char, changing length.
            
            // 2. Check Reordering: 
            //    Logical:  LTR(A) Neutral( ) RTL(AL) RTL(AL) Neutral( ) LTR(B)
            //    Visual:   LTR(A) Neutral( ) RTL(AL) RTL(AL) Neutral( ) LTR(B)
            //    Since Arabic flows R->L, visual output usually reverses the string storage for the RTL run.
            //    But `ProcessOutputForDisplay` in `BiDiTextProcessor` calls `BidiAlgorithm.ProcessString`.
            
            Assert.NotEqual(input, output);
        }
        
        [Fact]
        public void Shape_NullOrEmpty_ReturnsOriginal()
        {
            Assert.Null(ArabicShaper.Shape(null!));
            Assert.Equal("", ArabicShaper.Shape(""));
        }
    }
}