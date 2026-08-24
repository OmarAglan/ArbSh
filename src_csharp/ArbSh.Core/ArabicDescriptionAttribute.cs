namespace ArbSh.Core;

/// <summary>
/// يحدد الوصف العربي المختصر الذي يظهر في دليل أوامر أربش.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ArabicDescriptionAttribute : Attribute
{
    /// <summary>
    /// ينشئ وصفًا عربيًا لأمر واحد.
    /// </summary>
    /// <param name="description">وصف موجز وواضح للمستخدم.</param>
    public ArabicDescriptionAttribute(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Arabic description cannot be null or whitespace.", nameof(description));
        }

        Description = description;
    }

    /// <summary>
    /// الوصف العربي المختصر.
    /// </summary>
    public string Description { get; }
}
