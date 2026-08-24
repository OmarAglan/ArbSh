namespace ArbSh.Core.Processes;

/// <summary>
/// يصف آلية ملكية شجرة العملية المستخدمة فعليًا في محاولة التشغيل.
/// </summary>
public enum ExternalProcessTreeOwnershipMode
{
    /// <summary>لم تبدأ عملية أو لم تُكتسب ملكية شجرتها.</summary>
    None,

    /// <summary>ملكية صريحة عبر Windows Job Object.</summary>
    WindowsJobObject,

    /// <summary>
    /// قتل شجرة العملية الذي توفره .NET. هذا وضع انتقالي ظاهر على الأنظمة
    /// التي لم يكتمل فيها محول مجموعة العمليات بعد.
    /// </summary>
    DotNetProcessTree
}
