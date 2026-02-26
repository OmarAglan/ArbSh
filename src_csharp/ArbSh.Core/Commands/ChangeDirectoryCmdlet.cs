using System.IO;

namespace ArbSh.Core.Commands
{
    /// <summary>
    /// يغيّر المجلد الحالي للجلسة.
    /// </summary>
    [ArabicName("انتقل")]
    public sealed class ChangeDirectoryCmdlet : CmdletBase
    {
        /// <summary>
        /// المسار الهدف.
        /// </summary>
        [Parameter(Position = 0, HelpMessage = "المجلد المراد الانتقال إليه.")]
        [ArabicName("المسار")]
        public string? TargetPath { get; set; }

        /// <inheritdoc />
        public override void EndProcessing()
        {
            if (string.IsNullOrWhiteSpace(TargetPath))
            {
                WriteObject(ShellSessionContext.CurrentDirectory);
                return;
            }

            string resolvedPath;
            try
            {
                resolvedPath = ShellSessionContext.ResolvePath(TargetPath);
            }
            catch (Exception ex)
            {
                WriteObject(new PipelineObject($"المسار غير صالح: {TargetPath}. التفاصيل: {ex.Message}", isError: true));
                return;
            }

            if (File.Exists(resolvedPath))
            {
                WriteObject(new PipelineObject($"المسار يشير إلى ملف وليس مجلدًا: {TargetPath}", isError: true));
                return;
            }

            if (!Directory.Exists(resolvedPath))
            {
                WriteObject(new PipelineObject($"المجلد غير موجود: {TargetPath}", isError: true));
                return;
            }

            ShellSessionContext.CurrentDirectory = resolvedPath;
        }
    }
}
