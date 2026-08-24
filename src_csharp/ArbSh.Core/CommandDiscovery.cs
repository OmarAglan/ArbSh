using System.Reflection;
using ArbSh.Core.Models;

namespace ArbSh.Core
{
    /// <summary>
    /// مسؤول عن اكتشاف الأوامر المتاحة في المحرك.
    /// يعتمد أربش هنا على أسماء عربية فقط للأوامر القابلة للاستدعاء.
    /// </summary>
    public static class CommandDiscovery
    {
        private static readonly Lazy<IReadOnlyDictionary<string, Type>> CommandCache = new(
            BuildCache,
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// يعثر على نوع الأمر الموافق للاسم العربي المعطى.
        /// </summary>
        /// <param name="commandName">اسم الأمر العربي.</param>
        /// <returns>نوع الأمر أو null إذا لم يوجد.</returns>
        public static Type? Find(string commandName)
        {
            CommandCache.Value.TryGetValue(commandName, out Type? cmdletType);
            return cmdletType;
        }

        /// <summary>
        /// يرجع قاموسًا للّأوامر المكتشفة (الاسم العربي -> النوع).
        /// </summary>
        /// <returns>قاموس أوامر قابل للقراءة.</returns>
        public static IReadOnlyDictionary<string, Type> GetAllCommands()
        {
            return CommandCache.Value;
        }

        /// <summary>
        /// يرجع دليلًا عربيًا مرتبًا لكل أوامر المحرك وأمر الخروج الذي يملكه المضيف.
        /// </summary>
        public static IReadOnlyList<ArabicCommandInfo> GetArabicCatalog()
        {
            IEnumerable<ArabicCommandInfo> engineCommands = GetAllCommands()
                .Select(pair => new ArabicCommandInfo(
                    pair.Key,
                    pair.Value.GetCustomAttribute<ArabicDescriptionAttribute>()?.Description
                        ?? "لا يتوفر وصف لهذا الأمر.",
                    pair.Value));

            return engineCommands
                .Append(new ArabicCommandInfo(
                    "اخرج",
                    "ينهي جلسة أربش الحالية.",
                    ImplementingType: null))
                .OrderBy(command => command.Name, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// يبني مخزن الأوامر عبر فحص الأنواع في التجميعة الحالية.
        /// </summary>
        private static IReadOnlyDictionary<string, Type> BuildCache()
        {
            CoreConsole.WriteLine("DEBUG (Discovery): Building Arabic command cache...");
            var commandCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

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
                if (!commandCache.ContainsKey(arabicName))
                {
                    commandCache.Add(arabicName, type);
                    CoreConsole.WriteLine($"DEBUG (Discovery): Registered '{arabicName}' -> {type.FullName}");
                    continue;
                }

                if (commandCache[arabicName] != type)
                {
                    CoreConsole.WriteLine($"WARN (Discovery): Duplicate Arabic command '{arabicName}' between {type.FullName} and {commandCache[arabicName].FullName}.");
                }
            }

            CoreConsole.WriteLine($"DEBUG (Discovery): Arabic cache built with {commandCache.Count} command(s).");
            return commandCache;
        }
    }
}
