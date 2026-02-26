using System.Linq;

namespace ArbSh.Core.Commands
{
    /// <summary>
    /// يعرض قائمة الأوامر العربية المتاحة في أربش.
    /// </summary>
    [ArabicName("الأوامر")]
    public class GetCommandCmdlet : CmdletBase
    {
        /// <inheritdoc />
        public override void EndProcessing()
        {
            IReadOnlyDictionary<string, Type> allCommands = CommandDiscovery.GetAllCommands();
            if (allCommands.Count == 0)
            {
                WriteObject("لا توجد أوامر متاحة.");
                return;
            }

            IEnumerable<string> commandNames = allCommands.Keys
                .Append("اخرج")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.Ordinal);

            foreach (string commandName in commandNames)
            {
                WriteObject(commandName);
            }
        }
    }
}
