namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Represents the bidirectional character types as defined by the Unicode Bidirectional Algorithm (UAX #9).
    /// Values correspond to those used in the original C implementation for compatibility during porting.
    /// </summary>
    public enum BidiCharacterType
    {
        L = 0,    // Left-to-Right
        R = 1,    // Right-to-Left
        EN = 2,   // European Number
        ES = 3,   // European Number Separator
        ET = 4,   // European Number Terminator
        AN = 5,   // Arabic Number
        CS = 6,   // Common Number Separator
        B = 7,    // Paragraph Separator
        S = 8,    // Segment Separator
        WS = 9,   // Whitespace
        ON = 10,  // Other Neutral
        NSM = 11, // Non-spacing Mark
        AL = 12,  // Arabic Letter
        LRE = 13, // Left-to-Right Embedding
        RLE = 14, // Right-to-Left Embedding
        PDF = 15, // Pop Directional Format
        LRO = 16, // Left-to-Right Override
        RLO = 17, // Right-to-Left Override
        LRI = 18, // Left-to-Right Isolate
        RLI = 19, // Right-to-Left Isolate
        FSI = 20, // First Strong Isolate
        PDI = 21, // Pop Directional Isolate
        LRM = 22, // Left-to-Right Mark
        RLM = 23  // Right-to-Left Mark
    }
}
