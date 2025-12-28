using System;
using ICU4N.Text;

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// المسؤول عن تشكيل الأحرف العربية (تحويل الأحرف من الصورة المنطقية إلى صور العرض المتصلة).
    /// Responsible for Arabic character shaping (converting logical characters to connected presentation forms).
    /// Uses ICU4N library for robust standard-compliant shaping.
    /// </summary>
    public static class ArabicShaper
    {
        private static readonly ArabicShaping _shaper;

        static ArabicShaper()
        {
            // إعداد المشكل بالخيارات القياسية:
            // LETTERS_SHAPE: تحويل الأحرف إلى صور العرض المتصلة
            // TEXT_DIRECTION_LOGICAL: النص المدخل في ترتيب منطقي (وليس مرئي)
            
            // Initialize with standard options:
            // LettersShape: Convert letters to contextual presentation forms
            // TextDirectionLogical: Input is in logical memory order
            _shaper = new ArabicShaping(ArabicShaping.LettersShape | ArabicShaping.TextDirectionLogical);
        }

        /// <summary>
        /// يقوم بتشكيل النص العربي في السلسلة النصية المعطاة.
        /// Shapes the Arabic text in the given string, connecting letters based on context.
        /// </summary>
        /// <param name="text">النص المراد تشكيله. The logical text to shape.</param>
        /// <returns>النص بعد التشكيل. The text with presentation form characters.</returns>
        public static string Shape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            try
            {
                // Note: ICU's Shape method handles mixed text gracefully, 
                // affecting only Arabic characters within the appropriate ranges.
                return _shaper.Shape(text);
            }
            catch (Exception ex)
            {
                // في حالة حدوث خطأ، نعود للنص الأصلي ونقوم بتسجيل الخطأ
                // In case of error, fallback to original text and log warning
                System.Console.WriteLine($"WARNING (ArabicShaper): Failed to shape text: {ex.Message}");
                return text;
            }
        }
    }
}