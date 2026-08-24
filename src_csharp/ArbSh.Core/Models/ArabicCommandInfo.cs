namespace ArbSh.Core.Models;

/// <summary>
/// وصف عربي قابل للعرض لأمر مسجل في أربش.
/// </summary>
/// <param name="Name">اسم الاستدعاء العربي.</param>
/// <param name="Description">الوصف العربي المختصر.</param>
/// <param name="ImplementingType">نوع التنفيذ، أو null لأمر يملكه المضيف.</param>
public sealed record ArabicCommandInfo(
    string Name,
    string Description,
    Type? ImplementingType);
