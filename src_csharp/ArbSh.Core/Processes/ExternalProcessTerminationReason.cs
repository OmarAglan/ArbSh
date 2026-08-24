namespace ArbSh.Core.Processes;

/// <summary>
/// يصنف سبب انتهاء محاولة تشغيل عملية خارجية.
/// </summary>
public enum ExternalProcessTerminationReason
{
    /// <summary>
    /// بدأت العملية وخرجت بصورة طبيعية، سواء كان رمز الخروج صفرًا أم غير صفر.
    /// </summary>
    Exited,

    /// <summary>
    /// ألغى المستدعي العملية قبل اكتمالها.
    /// </summary>
    Cancelled,

    /// <summary>
    /// تعذر بدء العملية أصلًا.
    /// </summary>
    LaunchFailed,

    /// <summary>
    /// بدأت العملية لكن تعذر امتلاك شجرتها، فأوقفها ArbSh بدل المتابعة دون ضمان.
    /// </summary>
    OwnershipFailed
}
