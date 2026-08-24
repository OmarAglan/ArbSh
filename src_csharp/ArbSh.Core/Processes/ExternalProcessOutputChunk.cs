namespace ArbSh.Core.Processes;

/// <summary>
/// قطعة نصية وصلت من إحدى قناتي عملية خارجية قبل انتهاء العملية.
/// </summary>
/// <param name="Stream">القناة التي أنتجت النص.</param>
/// <param name="Text">النص المنطقي كما قُرئ من القناة.</param>
public sealed record ExternalProcessOutputChunk(
    ExternalProcessStream Stream,
    string Text);
