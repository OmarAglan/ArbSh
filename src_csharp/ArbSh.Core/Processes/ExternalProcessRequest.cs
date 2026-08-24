using System.Collections.ObjectModel;

namespace ArbSh.Core.Processes;

/// <summary>
/// طلب ثابت لتشغيل برنامج خارجي دون المرور بصدفة وسيطة.
/// </summary>
public sealed class ExternalProcessRequest
{
    /// <summary>
    /// ينشئ طلب عملية منظمة ويلتقط نسخًا ثابتة من الوسائط وتغييرات البيئة.
    /// </summary>
    /// <param name="executable">اسم البرنامج أو مساره.</param>
    /// <param name="arguments">الوسائط المنطقية المنفصلة، بما فيها الوسيط الفارغ.</param>
    /// <param name="workingDirectory">مجلد العمل، أو <see langword="null"/> لوراثة مجلد المضيف.</param>
    /// <param name="environmentChanges">تغييرات البيئة؛ القيمة الفارغة تحذف المتغير الموروث.</param>
    /// <param name="inheritEnvironment">هل تبدأ العملية بنسخة من بيئة المضيف.</param>
    /// <param name="standardInputMode">طريقة توصيل الإدخال القياسي.</param>
    /// <param name="standardInputText">النص المكتوب عندما يكون وضع الإدخال <see cref="ExternalProcessInputMode.Text"/>.</param>
    public ExternalProcessRequest(
        string executable,
        IEnumerable<string>? arguments = null,
        string? workingDirectory = null,
        IEnumerable<KeyValuePair<string, string?>>? environmentChanges = null,
        bool inheritEnvironment = true,
        ExternalProcessInputMode standardInputMode = ExternalProcessInputMode.Closed,
        string? standardInputText = null)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new ArgumentException("يجب تحديد اسم البرنامج أو مساره.", nameof(executable));
        }

        if (workingDirectory is not null && string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new ArgumentException("لا يجوز أن يكون مجلد العمل فارغًا.", nameof(workingDirectory));
        }

        if (!Enum.IsDefined(standardInputMode))
        {
            throw new ArgumentOutOfRangeException(nameof(standardInputMode));
        }

        if (standardInputMode is not ExternalProcessInputMode.Text && standardInputText is not null)
        {
            throw new ArgumentException(
                "لا يمكن تحديد نص الإدخال إلا عند اختيار وضع الإدخال النصي.",
                nameof(standardInputText));
        }

        string[] argumentSnapshot = arguments?.Select(
            argument => argument ?? throw new ArgumentException(
                "لا يجوز أن تحتوي قائمة الوسائط على قيمة فارغة null.",
                nameof(arguments))).ToArray() ?? [];

        var environmentSnapshot = new Dictionary<string, string?>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        if (environmentChanges is not null)
        {
            foreach (KeyValuePair<string, string?> change in environmentChanges)
            {
                ValidateEnvironmentChange(change, nameof(environmentChanges));
                environmentSnapshot[change.Key] = change.Value;
            }
        }

        Executable = executable;
        Arguments = Array.AsReadOnly(argumentSnapshot);
        WorkingDirectory = workingDirectory;
        EnvironmentChanges = new ReadOnlyDictionary<string, string?>(environmentSnapshot);
        InheritEnvironment = inheritEnvironment;
        StandardInputMode = standardInputMode;
        StandardInputText = standardInputMode is ExternalProcessInputMode.Text
            ? standardInputText ?? string.Empty
            : null;
    }

    /// <summary>
    /// اسم البرنامج أو مساره كما قدمه المستدعي.
    /// </summary>
    public string Executable { get; }

    /// <summary>
    /// الوسائط المنطقية المنفصلة بالترتيب.
    /// </summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>
    /// مجلد العمل الصريح، أو <see langword="null"/> لوراثة مجلد المضيف.
    /// </summary>
    public string? WorkingDirectory { get; }

    /// <summary>
    /// تغييرات البيئة؛ تعني القيمة <see langword="null"/> حذف المتغير.
    /// </summary>
    public IReadOnlyDictionary<string, string?> EnvironmentChanges { get; }

    /// <summary>
    /// يحدد ما إذا كانت العملية تبدأ ببيئة المضيف قبل تطبيق التغييرات.
    /// </summary>
    public bool InheritEnvironment { get; }

    /// <summary>
    /// طريقة توصيل الإدخال القياسي.
    /// </summary>
    public ExternalProcessInputMode StandardInputMode { get; }

    /// <summary>
    /// نص الإدخال القياسي في الوضع النصي، وإلا تكون القيمة <see langword="null"/>.
    /// </summary>
    public string? StandardInputText { get; }

    private static void ValidateEnvironmentChange(
        KeyValuePair<string, string?> change,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(change.Key)
            || change.Key.Contains('=')
            || change.Key.Contains('\0'))
        {
            throw new ArgumentException("اسم متغير البيئة غير صالح.", parameterName);
        }

        if (change.Value?.Contains('\0') is true)
        {
            throw new ArgumentException("قيمة متغير البيئة تحتوي محرفًا صفريًا غير صالح.", parameterName);
        }
    }
}
