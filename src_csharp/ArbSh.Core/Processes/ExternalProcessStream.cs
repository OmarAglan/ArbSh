namespace ArbSh.Core.Processes;

/// <summary>
/// يحدد قناة النص التي أنتجت قطعة مخرجات من عملية خارجية.
/// </summary>
public enum ExternalProcessStream
{
    /// <summary>
    /// قناة الخرج القياسي stdout.
    /// </summary>
    StandardOutput,

    /// <summary>
    /// قناة الخطأ القياسي stderr.
    /// </summary>
    StandardError
}
