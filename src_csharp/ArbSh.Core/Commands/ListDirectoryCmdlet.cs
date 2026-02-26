using System.IO;
using System.Linq;

namespace ArbSh.Core.Commands
{
    /// <summary>
    /// يعرض محتويات المجلد الحالي أو مجلد محدد.
    /// </summary>
    [ArabicName("اعرض")]
    public sealed class ListDirectoryCmdlet : CmdletBase
    {
        /// <summary>
        /// المسار المراد عرضه.
        /// </summary>
        [Parameter(Position = 0, HelpMessage = "المسار المطلوب عرض محتوياته.")]
        [ArabicName("المسار")]
        public string? TargetPath { get; set; }

        /// <summary>
        /// يحدد إن كان سيتم عرض العناصر المخفية.
        /// </summary>
        [Parameter(HelpMessage = "عرض الملفات/المجلدات المخفية.")]
        [ArabicName("مخفي")]
        public bool IncludeHidden { get; set; }

        /// <inheritdoc />
        public override void EndProcessing()
        {
            string logicalTarget = string.IsNullOrWhiteSpace(TargetPath)
                ? ShellSessionContext.CurrentDirectory
                : TargetPath!;

            string resolvedPath;
            try
            {
                resolvedPath = ShellSessionContext.ResolvePath(logicalTarget);
            }
            catch (Exception ex)
            {
                WriteObject(new PipelineObject($"المسار غير صالح: {logicalTarget}. التفاصيل: {ex.Message}", isError: true));
                return;
            }

            if (File.Exists(resolvedPath))
            {
                WriteObject(Path.GetFileName(resolvedPath));
                return;
            }

            if (!Directory.Exists(resolvedPath))
            {
                WriteObject(new PipelineObject($"المجلد غير موجود: {logicalTarget}", isError: true));
                return;
            }

            IEnumerable<FileSystemInfo> entries;
            try
            {
                entries = new DirectoryInfo(resolvedPath).EnumerateFileSystemInfos();
            }
            catch (Exception ex)
            {
                WriteObject(new PipelineObject($"تعذّر قراءة المجلد: {logicalTarget}. التفاصيل: {ex.Message}", isError: true));
                return;
            }

            if (!IncludeHidden)
            {
                entries = entries.Where(entry =>
                    (entry.Attributes & FileAttributes.Hidden) == 0 &&
                    (entry.Attributes & FileAttributes.System) == 0);
            }

            entries = entries
                .OrderBy(entry => entry is DirectoryInfo ? 0 : 1)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase);

            foreach (FileSystemInfo entry in entries)
            {
                string output = entry is DirectoryInfo
                    ? $"{entry.Name}/"
                    : entry.Name;

                WriteObject(output);
            }
        }
    }
}
