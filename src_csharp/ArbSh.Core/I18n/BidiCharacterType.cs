namespace ArbSh.Core.I18n
{
    /// <summary>
    /// Represents the bidirectional character types as defined by the Unicode Bidirectional Algorithm (UAX #9).
    /// These values should align with the classifications needed by the BiDi algorithm steps.
    /// </summary>
    public enum BidiCharacterType
    {
        // Strong Types
        L,    // Left-to-Right
        R,    // Right-to-Left
        AL,   // Arabic Letter (Right-to-Left)

        // Weak Types
        EN,   // European Number
        ES,   // European Number Separator
        ET,   // European Number Terminator
        AN,   // Arabic Number
        CS,   // Common Number Separator
        NSM,  // Non-Spacing Mark

        // Neutral Types
        B,    // Paragraph Separator
        S,    // Segment Separator
        WS,   // Whitespace
        ON,   // Other Neutral

        // Explicit Formatting Codes (UAX #9 X1-X8, X10)
        LRE,  // Left-to-Right Embedding
        RLE,  // Right-to-Left Embedding
        PDF,  // Pop Directional Format
        LRO,  // Left-to-Right Override
        RLO,  // Right-to-Left Override
        LRI,  // Left-to-Right Isolate
        RLI,  // Right-to-Left Isolate
        FSI,  // First Strong Isolate
        PDI,  // Pop Directional Isolate

        // Boundary Neutral (UAX #9 N0).
        // LRM, RLM, and other control/format characters are often BN.
        BN
        // Note: LRM (U+200E) and RLM (U+200F) will be classified as BN by GetCharType.
        // Their special behavior is handled in later BiDi rules (e.g., UAX #9 BD9, or how they affect N0).
    }
}
