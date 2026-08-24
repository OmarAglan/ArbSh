namespace ArbSh.Core.Processes;

/// <summary>
/// يشغّل البرامج الخارجية من طلبات منظمة دون تحليل نص صدفة.
/// </summary>
public interface IExternalProcessRunner
{
    /// <summary>
    /// يشغّل العملية ويلتقط stdout وstderr ورمز الخروج أو تصنيف الإلغاء والفشل.
    /// </summary>
    /// <param name="request">طلب التشغيل الثابت.</param>
    /// <param name="cancellationToken">إشارة إلغاء العملية.</param>
    /// <returns>نتيجة العملية بعد اكتمال جمع المخرجات.</returns>
    Task<ExternalProcessResult> RunAsync(
        ExternalProcessRequest request,
        CancellationToken cancellationToken = default);
}
