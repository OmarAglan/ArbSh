namespace ArbSh.Core.Processes;

/// <summary>
/// النتيجة الثابتة لمحاولة تشغيل عملية خارجية منظمة.
/// </summary>
public sealed class ExternalProcessResult
{
    internal ExternalProcessResult(
        ExternalProcessTerminationReason terminationReason,
        int? exitCode,
        string standardOutput,
        string standardError,
        TimeSpan duration,
        string? failureMessage,
        ExternalProcessTreeOwnershipMode treeOwnershipMode)
    {
        TerminationReason = terminationReason;
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
        Duration = duration;
        FailureMessage = failureMessage;
        TreeOwnershipMode = treeOwnershipMode;
    }

    /// <summary>
    /// سبب انتهاء محاولة التشغيل.
    /// </summary>
    public ExternalProcessTerminationReason TerminationReason { get; }

    /// <summary>
    /// رمز خروج العملية عند خروجها طبيعيًا، وإلا تكون القيمة <see langword="null"/>.
    /// </summary>
    public int? ExitCode { get; }

    /// <summary>
    /// النص الكامل الملتقط من stdout بترميز UTF-8.
    /// </summary>
    public string StandardOutput { get; }

    /// <summary>
    /// النص الكامل الملتقط من stderr بترميز UTF-8.
    /// </summary>
    public string StandardError { get; }

    /// <summary>
    /// مدة محاولة التشغيل منذ ما قبل البدء حتى جمع المخرجات.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// وصف فشل البنية التحتية عند تعذر البدء أو الملكية أو الإنهاء المضمون،
    /// وإلا تكون القيمة <see langword="null"/>.
    /// </summary>
    public string? FailureMessage { get; }

    /// <summary>
    /// آلية ملكية شجرة العملية التي استُخدمت فعليًا.
    /// </summary>
    public ExternalProcessTreeOwnershipMode TreeOwnershipMode { get; }

    /// <summary>
    /// يحدد أن العملية بدأت وخرجت برمز صفر.
    /// </summary>
    public bool Succeeded => TerminationReason is ExternalProcessTerminationReason.Exited
        && ExitCode is 0;
}
