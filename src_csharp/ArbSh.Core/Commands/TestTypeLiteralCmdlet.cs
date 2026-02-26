using System;

namespace ArbSh.Core.Commands
{
    /// <summary>
    /// أمر لاختبار تحويلات نوع المعاملات باستخدام صيغ الأنواع.
    /// </summary>
    [ArabicName("اختبار-نوع")]
    public class TestTypeLiteralCmdlet : CmdletBase
    {
        /// <summary>
        /// قيمة عددية صحيحة.
        /// </summary>
        [Parameter(Position = 0, HelpMessage = "قيمة عددية صحيحة.")]
        [ArabicName("عدد-صحيح")]
        public int IntValue { get; set; }

        /// <summary>
        /// قيمة نصية.
        /// </summary>
        [Parameter(Position = 1, HelpMessage = "قيمة نصية.")]
        [ArabicName("نص")]
        public string? StringValue { get; set; }

        /// <summary>
        /// قيمة منطقية.
        /// </summary>
        [Parameter(Position = 2, HelpMessage = "قيمة منطقية.")]
        [ArabicName("منطقي")]
        public bool BoolValue { get; set; }

        /// <summary>
        /// قيمة عشرية.
        /// </summary>
        [Parameter(Position = 3, HelpMessage = "قيمة عشرية.")]
        [ArabicName("عشري")]
        public double DoubleValue { get; set; }

        /// <summary>
        /// قيمة تاريخ/وقت.
        /// </summary>
        [Parameter(Position = 4, HelpMessage = "قيمة تاريخ/وقت.")]
        [ArabicName("تاريخ")]
        public DateTime DateTimeValue { get; set; }

        /// <summary>
        /// مصفوفة من الأعداد الصحيحة.
        /// </summary>
        [Parameter(HelpMessage = "مصفوفة أعداد صحيحة.")]
        [ArabicName("مصفوفة-أعداد")]
        public int[]? IntArray { get; set; }

        /// <summary>
        /// لون من تعداد ConsoleColor.
        /// </summary>
        [Parameter(HelpMessage = "لون من ConsoleColor.")]
        [ArabicName("لون")]
        public ConsoleColor ColorValue { get; set; }

        public override void ProcessRecord(PipelineObject? input)
        {
            // This cmdlet primarily works with parameters bound before ProcessRecord is called.
            // We'll output the bound values in EndProcessing.
        }

        public override void EndProcessing()
        {
            WriteObject($"عدد-صحيح: {IntValue} (النوع: {IntValue.GetType().Name})");
            WriteObject($"نص: '{StringValue}' (النوع: {StringValue?.GetType().Name ?? "null"})");
            WriteObject($"منطقي: {BoolValue} (النوع: {BoolValue.GetType().Name})");
            WriteObject($"عشري: {DoubleValue} (النوع: {DoubleValue.GetType().Name})");
            WriteObject($"تاريخ: {DateTimeValue} (النوع: {DateTimeValue.GetType().Name})");
            WriteObject($"لون: {ColorValue} (النوع: {ColorValue.GetType().Name})");

            if (IntArray != null && IntArray.Length > 0)
            {
                WriteObject($"مصفوفة-أعداد: [{string.Join(", ", IntArray)}] (النوع: {IntArray.GetType().Name}, الطول: {IntArray.Length})");
            }
            else
            {
                WriteObject("مصفوفة-أعداد: فارغة أو null");
            }
        }
    }
}

