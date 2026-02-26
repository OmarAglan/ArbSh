using System;
using System.Linq;

namespace ArbSh.Core.Commands
{
    /// <summary>
    /// أمر لاختبار ربط المصفوفات عبر المعاملات الموضعية.
    /// </summary>
    [ArabicName("اختبار-مصفوفة")]
    public class TestArrayBindingCmdlet : CmdletBase
    {
        /// <summary>
        /// قائمة النصوص المطلوب ربطها كمصفوفة.
        /// </summary>
        [Parameter(Position = 0, HelpMessage = "يستقبل عدة نصوص.")]
        [ArabicName("نصوص")]
        public string[]? InputStrings { get; set; }

        /// <summary>
        /// مبدل اختياري للاختبار.
        /// </summary>
        [Parameter(HelpMessage = "مبدل اختياري.")]
        [ArabicName("مبدل")]
        public bool MySwitch { get; set; }

        public override void ProcessRecord(PipelineObject? input)
        {
            // This cmdlet primarily works with parameters bound before ProcessRecord is called.
            // We'll output the bound array in EndProcessing.
        }

        public override void EndProcessing()
        {
            WriteObject($"قيمة المبدل: {MySwitch}");
            if (InputStrings != null && InputStrings.Any())
            {
                WriteObject($"تم استلام {InputStrings.Length} عنصر/عناصر نصية:");
                for (int i = 0; i < InputStrings.Length; i++)
                {
                    WriteObject($"  [{i}]: '{InputStrings[i]}'");
                }
            }
            else
            {
                WriteObject("لم يتم استلام أي نصوص.");
            }
        }
    }
}

