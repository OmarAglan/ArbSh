using System.Reflection;
using System.Text;

namespace ArbSh.Core.Commands
{
    /// <summary>
    /// يعرض المساعدة العامة أو تفاصيل أمر عربي محدد.
    /// </summary>
    [ArabicName("مساعدة")]
    public class GetHelpCmdlet : CmdletBase
    {
        /// <summary>
        /// اسم الأمر المطلوب عرض المساعدة له.
        /// </summary>
        [Parameter(Position = 0, HelpMessage = "اسم الأمر المطلوب.")]
        [ArabicName("الأمر")]
        public string? CommandName { get; set; }

        /// <summary>
        /// عرض تفاصيل أوسع عن المعاملات.
        /// </summary>
        [Parameter(HelpMessage = "عرض التفاصيل الكاملة.")]
        [ArabicName("كامل")]
        public bool Full { get; set; }

        /// <inheritdoc />
        public override void EndProcessing()
        {
            if (!string.IsNullOrWhiteSpace(CommandName))
            {
                if (string.Equals(CommandName, "اخرج", StringComparison.Ordinal))
                {
                    WriteObject("\nالاسم");
                    WriteObject("  اخرج");
                    WriteObject("\nالوصف");
                    WriteObject("  ينهي جلسة أربش الحالية في المضيف.");
                    WriteObject("\nالاستخدام");
                    WriteObject("  اخرج");
                    return;
                }

                Type? targetCmdletType = CommandDiscovery.Find(CommandName);
                if (targetCmdletType != null)
                {
                    DisplayCommandHelp(targetCmdletType);
                }
                else
                {
                    WriteObject(new PipelineObject($"تعذّر العثور على الأمر: {CommandName}", isError: true));
                }

                return;
            }

            WriteObject("استخدام المساعدة:");
            WriteObject("  مساعدة <الأمر>");
            WriteObject("مثال:");
            WriteObject("  مساعدة الأوامر");

            if (!Full)
            {
                return;
            }

            WriteObject("\nالأوامر المتاحة:");
            IReadOnlyDictionary<string, Type> allCommands = CommandDiscovery.GetAllCommands();
            IEnumerable<string> commandNames = allCommands.Keys
                .Append("اخرج")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.Ordinal);

            foreach (string commandName in commandNames)
            {
                WriteObject($"  {commandName}");
            }
        }

        private void DisplayCommandHelp(Type cmdletType)
        {
            var helpBuilder = new StringBuilder();

            string commandName = CommandDiscovery.GetAllCommands()
                .FirstOrDefault(kvp => kvp.Value == cmdletType).Key;

            if (string.IsNullOrWhiteSpace(commandName))
            {
                ArabicNameAttribute? nameAttr = cmdletType.GetCustomAttribute<ArabicNameAttribute>();
                commandName = nameAttr?.Name ?? cmdletType.Name;
            }

            helpBuilder.AppendLine("\nالاسم");
            helpBuilder.AppendLine($"  {commandName}");

            helpBuilder.AppendLine("\nالصيغة");
            helpBuilder.Append($"  {commandName}");

            List<(PropertyInfo Property, ParameterAttribute Attr)> parameters = cmdletType.GetProperties()
                .Select(p => (Property: p, Attr: p.GetCustomAttribute<ParameterAttribute>()))
                .Where(x => x.Attr != null)
                .Select(x => (Property: x.Property, Attr: x.Attr!))
                .OrderBy(x => x.Attr.Position >= 0 ? x.Attr.Position : int.MaxValue)
                .ThenBy(x => x.Property.Name)
                .ToList();

            foreach ((PropertyInfo property, ParameterAttribute attr) in parameters)
            {
                ArabicNameAttribute? arabicNameAttr = property.GetCustomAttribute<ArabicNameAttribute>();
                string parameterName = arabicNameAttr?.Name ?? property.Name;

                string paramSyntax = $" [-{parameterName}";
                if (property.PropertyType != typeof(bool))
                {
                    paramSyntax += $" <{property.PropertyType.Name}>";
                }

                paramSyntax += "]";
                helpBuilder.Append(paramSyntax);
            }

            helpBuilder.AppendLine();

            if (!Full || parameters.Count == 0)
            {
                WriteObject(helpBuilder.ToString());
                return;
            }

            helpBuilder.AppendLine("\nالمعاملات");
            foreach ((PropertyInfo property, ParameterAttribute attr) in parameters)
            {
                ArabicNameAttribute? arabicNameAttr = property.GetCustomAttribute<ArabicNameAttribute>();
                string parameterName = arabicNameAttr?.Name ?? property.Name;

                helpBuilder.AppendLine($"  -{parameterName} <{property.PropertyType.Name}>");
                if (!string.IsNullOrWhiteSpace(attr.HelpMessage))
                {
                    helpBuilder.AppendLine($"    {attr.HelpMessage}");
                }

                helpBuilder.AppendLine($"    إلزامي: {(attr.Mandatory ? "نعم" : "لا")}");
                helpBuilder.AppendLine($"    الموضع: {(attr.Position >= 0 ? attr.Position.ToString() : "مسماة")}");
                helpBuilder.AppendLine($"    من الدفق: {(attr.ValueFromPipeline ? "نعم (بالقيمة)" : (attr.ValueFromPipelineByPropertyName ? "نعم (بالاسم)" : "لا"))}");
                helpBuilder.AppendLine();
            }

            WriteObject(helpBuilder.ToString());
        }
    }
}
