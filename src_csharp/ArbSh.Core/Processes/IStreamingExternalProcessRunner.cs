namespace ArbSh.Core.Processes;

/// <summary>
/// يضيف البث الحي للمخرجات إلى عقد تشغيل العمليات المنظمة.
/// </summary>
public interface IStreamingExternalProcessRunner : IExternalProcessRunner
{
    /// <summary>
    /// يشغّل العملية ويرسل stdout وstderr إلى المستقبل أثناء التنفيذ، مع
    /// الاحتفاظ بنسخة كاملة منهما في النتيجة للتوافق مع العقد الأساسي.
    /// </summary>
    /// <param name="request">طلب التشغيل الثابت.</param>
    /// <param name="outputSink">مستقبل القطع الحية.</param>
    /// <param name="cancellationToken">إشارة إلغاء العملية.</param>
    Task<ExternalProcessResult> RunStreamingAsync(
        ExternalProcessRequest request,
        IExternalProcessOutputSink outputSink,
        CancellationToken cancellationToken = default);
}
