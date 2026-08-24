namespace ArbSh.Core.Processes;

/// <summary>
/// يستقبل مخرجات العملية الخارجية تدريجيًا أثناء بقائها قيد التشغيل.
/// </summary>
/// <remarks>
/// قد تستدعي قناتا stdout وstderr هذا المستقبل بالتوازي، لذلك يجب أن يكون
/// التنفيذ آمنًا للاستخدام المتزامن. يحافظ كل تيار على ترتيبه الداخلي، أما
/// حدود القطع فليست جزءًا ثابتًا من العقد.
/// </remarks>
public interface IExternalProcessOutputSink
{
    /// <summary>
    /// يستقبل قطعة واحدة غير فارغة من مخرجات العملية.
    /// </summary>
    /// <param name="chunk">القناة والنص المقروء منها.</param>
    /// <param name="cancellationToken">إشارة إلغاء انتظار المستقبل نفسه.</param>
    ValueTask WriteAsync(
        ExternalProcessOutputChunk chunk,
        CancellationToken cancellationToken = default);
}
