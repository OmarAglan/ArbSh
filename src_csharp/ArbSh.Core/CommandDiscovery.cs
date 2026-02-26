using System.Reflection;

namespace ArbSh.Core
{
    /// <summary>
    /// مسؤول عن اكتشاف الأوامر المتاحة في المحرك.
    /// يعتمد أربش هنا على أسماء عربية فقط للأوامر القابلة للاستدعاء.
    /// </summary>
    public static class CommandDiscovery
    {
        private static Dictionary<string, Type>? _commandCache;

        /// <summary>
        /// يعثر على نوع الأمر الموافق للاسم العربي المعطى.
        /// </summary>
        /// <param name="commandName">اسم الأمر العربي.</param>
        /// <returns>نوع الأمر أو null إذا لم يوجد.</returns>
        public static Type? Find(string commandName)
        {
            if (_commandCache == null)
            {
                BuildCache();
            }

            _commandCache!.TryGetValue(commandName, out Type? cmdletType);
            return cmdletType;
        }

        /// <summary>
        /// يرجع قاموسًا للّأوامر المكتشفة (الاسم العربي -> النوع).
        /// </summary>
        /// <returns>قاموس أوامر قابل للقراءة.</returns>
        public static IReadOnlyDictionary<string, Type> GetAllCommands()
        {
            if (_commandCache == null)
            {
                BuildCache();
            }

            return _commandCache!;
        }

        /// <summary>
        /// يبني مخزن الأوامر عبر فحص الأنواع في التجميعة الحالية.
        /// </summary>
        private static void BuildCache()
        {
            CoreConsole.WriteLine("DEBUG (Discovery): Building Arabic command cache...");
            _commandCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            IEnumerable<Type> cmdletTypes = currentAssembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(CmdletBase)) && !t.IsAbstract);

            foreach (Type type in cmdletTypes)
            {
                ArabicNameAttribute? arabicNameAttr = type.GetCustomAttribute<ArabicNameAttribute>();
                if (arabicNameAttr == null)
                {
                    CoreConsole.WriteLine($"DEBUG (Discovery): Skipping '{type.Name}' (no ArabicName).");
                    continue;
                }

                string arabicName = arabicNameAttr.Name;
                if (!_commandCache.ContainsKey(arabicName))
                {
                    _commandCache.Add(arabicName, type);
                    CoreConsole.WriteLine($"DEBUG (Discovery): Registered '{arabicName}' -> {type.FullName}");
                    continue;
                }

                if (_commandCache[arabicName] != type)
                {
                    CoreConsole.WriteLine($"WARN (Discovery): Duplicate Arabic command '{arabicName}' between {type.FullName} and {_commandCache[arabicName].FullName}.");
                }
            }

            CoreConsole.WriteLine($"DEBUG (Discovery): Arabic cache built with {_commandCache.Count} command(s).");
        }
    }
}
