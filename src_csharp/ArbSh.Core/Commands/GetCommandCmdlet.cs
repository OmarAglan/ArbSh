using ArbSh.Core.Models;

namespace ArbSh.Core.Commands
{
    /// <summary>
    /// يعرض قائمة الأوامر العربية المتاحة في أربش.
    /// </summary>
    [ArabicName("الأوامر")]
    [ArabicDescription("يعرض جميع أوامر أربش العربية ووصف كل أمر.")]
    public class GetCommandCmdlet : CmdletBase
    {
        /// <inheritdoc />
        public override void EndProcessing()
        {
            WriteObject("أوامر أربش العربية:");
            foreach (ArabicCommandInfo command in CommandDiscovery.GetArabicCatalog())
            {
                WriteObject($"  {command.Name} — {command.Description}");
            }

            WriteObject("");
            WriteObject("للتفاصيل: مساعدة <اسم-الأمر>");
        }
    }
}
