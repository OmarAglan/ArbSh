namespace ArbSh.Core.Processes;

/// <summary>
/// يحدد طريقة توصيل الإدخال القياسي بالعملية الخارجية المنظمة.
/// </summary>
public enum ExternalProcessInputMode
{
    /// <summary>
    /// يغلق الإدخال القياسي فور بدء العملية. وهذا هو الوضع الافتراضي الآمن.
    /// </summary>
    Closed,

    /// <summary>
    /// يكتب النص المحدد بترميز UTF-8 ثم يغلق الإدخال القياسي.
    /// </summary>
    Text,

    /// <summary>
    /// يورث مقبض الإدخال القياسي من عملية ArbSh المضيفة.
    /// </summary>
    Inherit
}
