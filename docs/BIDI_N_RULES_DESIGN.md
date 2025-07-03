# BiDi Algorithm N Rules (N0-N2) Implementation Design

## Overview

This document outlines the design and implementation approach for UAX #9 N rules (N0-N2) for resolving neutral and isolate formatting types in the ArbSh BiDi algorithm implementation.

## N Rules Summary

The N rules resolve neutral types (NI) which include:
- **ON** (Other Neutral): Punctuation, symbols, brackets
- **WS** (Whitespace): Spaces, tabs
- **S** (Segment Separator): Tab character in some contexts
- **B** (Paragraph Separator): Line breaks, paragraph separators
- **BN** (Boundary Neutral): Formatting characters (handled specially)

### Rule N0: Bracket Pair Processing

**Purpose**: Process bracket pairs as units so both opening and closing brackets resolve to the same direction.

**Algorithm**:
1. Identify bracket pairs using BD16 algorithm (63-element stack)
2. For each bracket pair in logical order:
   - If strong type matching embedding direction found inside → set both brackets to embedding direction
   - Else if strong type opposite embedding direction found inside:
     - Check context before opening bracket for same strong type
     - If context matches → set both brackets to that direction
     - Else → set both brackets to embedding direction
   - Else (no strong types inside) → leave brackets unchanged for N1/N2
3. NSM characters following brackets changed by N0 adopt the bracket's new type

### Rule N1: Surrounding Strong Type Resolution

**Purpose**: Resolve NI sequences based on surrounding strong types.

**Algorithm**:
- If NI sequence surrounded by same strong type (L...NI...L or R...NI...R) → change NI to that type
- EN and AN are treated as R for this rule
- Use sos/eos at isolating run sequence boundaries

### Rule N2: Embedding Direction Fallback

**Purpose**: Resolve remaining NI characters to embedding direction.

**Algorithm**:
- Any remaining NI → embedding direction (even level = L, odd level = R)

## Data Structures

### BracketPair Structure
```csharp
public struct BracketPair
{
    public int OpeningPosition;    // Text position of opening bracket
    public int ClosingPosition;    // Text position of closing bracket
    public char OpeningChar;       // Opening bracket character
    public char ClosingChar;       // Closing bracket character
}
```

### BracketStackEntry Structure
```csharp
public struct BracketStackEntry
{
    public char BracketChar;       // Bidi_Paired_Bracket property value
    public int TextPosition;       // Position in isolating run sequence
}
```

## Implementation Architecture

### Integration with Existing Pipeline

The N rules will be integrated into the existing `ApplyWRules` method flow:

```csharp
private static void ProcessIsolatingRunSequence(IsolatingRunSequence sequence)
{
    // W rules (already implemented)
    ApplyWRulesToSequence(sequence);
    
    // N rules (new implementation)
    ApplyNRulesToSequence(sequence);
}
```

### Core N Rules Methods

1. **ApplyNRulesToSequence(IsolatingRunSequence sequence)**
   - Main entry point for N rules processing
   - Calls N0, N1, N2 in sequence

2. **ApplyN0_BracketPairs(IsolatingRunSequence sequence)**
   - Identify bracket pairs using BD16
   - Process each pair according to N0 logic
   - Handle NSM characters following changed brackets

3. **ApplyN1_SurroundingStrongTypes(IsolatingRunSequence sequence)**
   - Find NI sequences
   - Check surrounding strong types (including sos/eos)
   - Resolve NI to surrounding type if consistent

4. **ApplyN2_EmbeddingDirection(IsolatingRunSequence sequence)**
   - Resolve remaining NI to embedding direction

### Helper Methods

1. **IdentifyBracketPairs(IsolatingRunSequence sequence) → List<BracketPair>**
   - Implement BD16 algorithm with 63-element stack
   - Handle canonical equivalence (U+3008/U+3009 ↔ U+2329/U+232A)
   - Return sorted list by opening bracket position

2. **IsOpeningBracket(char c, BidiCharacterType currentType) → bool**
   - Check BD14: Bidi_Paired_Bracket_Type = Open AND current type = ON

3. **IsClosingBracket(char c, BidiCharacterType currentType) → bool**
   - Check BD15: Bidi_Paired_Bracket_Type = Close AND current type = ON

4. **GetBracketPairValue(char c) → char**
   - Get Bidi_Paired_Bracket property value
   - Handle canonical equivalence

5. **FindStrongTypeInBrackets(IsolatingRunSequence sequence, int start, int end) → BidiCharacterType?**
   - Search for L or R within bracket pair
   - Treat EN/AN as R
   - Skip characters not in isolating run sequence

6. **FindPrecedingStrongType(IsolatingRunSequence sequence, int position) → BidiCharacterType**
   - Search backward for first L or R (treat EN/AN as R)
   - Return sos if none found

7. **GetEmbeddingDirection(int level) → BidiCharacterType**
   - Even level → L, Odd level → R

## Unicode Character Database Integration

### Bracket Properties Required

The implementation needs access to Unicode bracket properties:
- **Bidi_Paired_Bracket**: The paired bracket character
- **Bidi_Paired_Bracket_Type**: Open, Close, or None

### ICU4N Integration

**Note**: ICU4N does not provide BIDI_PAIRED_BRACKET and BIDI_PAIRED_BRACKET_TYPE properties. We need to implement a fallback using hardcoded bracket mappings.

```csharp
// Fallback bracket property implementation
private static readonly Dictionary<char, char> BracketPairs = new Dictionary<char, char>
{
    // Basic brackets
    { '(', ')' }, { ')', '(' },
    { '[', ']' }, { ']', '[' },
    { '{', '}' }, { '}', '{' },

    // Angle brackets (canonical equivalence)
    { '⟨', '⟩' }, { '⟩', '⟨' },  // U+27E8, U+27E9
    { '〈', '〉' }, { '〉', '〈' },  // U+3008, U+3009 (canonical equivalent)

    // Additional Unicode brackets
    { '⟦', '⟧' }, { '⟧', '⟦' },  // U+27E6, U+27E7
    { '⟪', '⟫' }, { '⟫', '⟪' },  // U+27EA, U+27EB
    { '⦃', '⦄' }, { '⦄', '⦃' },  // U+2983, U+2984
    { '⦅', '⦆' }, { '⦆', '⦅' },  // U+2985, U+2986
};

private static readonly HashSet<char> OpeningBrackets = new HashSet<char>
{
    '(', '[', '{', '⟨', '〈', '⟦', '⟪', '⦃', '⦅'
};

public static char GetPairedBracket(char c)
{
    return BracketPairs.TryGetValue(c, out char paired) ? paired : c;
}

public static bool IsOpeningBracket(char c)
{
    return OpeningBrackets.Contains(c);
}

public static bool IsClosingBracket(char c)
{
    return BracketPairs.ContainsKey(c) && !OpeningBrackets.Contains(c);
}
```

## Testing Strategy

### Unit Test Categories

1. **N0 Bracket Pair Tests**
   - Simple bracket pairs with strong types inside
   - Mixed strong types (take embedding direction)
   - Context-dependent resolution
   - Nested bracket pairs
   - Malformed bracket sequences
   - NSM handling after bracket resolution

2. **N1 Surrounding Strong Type Tests**
   - L...NI...L → L L L
   - R...NI...R → R R R
   - Mixed surrounding types (no change)
   - EN/AN treated as R
   - sos/eos boundary handling

3. **N2 Embedding Direction Tests**
   - Remaining NI at even levels → L
   - Remaining NI at odd levels → R

4. **Integration Tests**
   - Complete N0→N1→N2 processing
   - Integration with existing W rules
   - Complex mixed text scenarios

### Test Data Examples

```csharp
// N0 bracket pair test
"a(b)c" // LTR context: brackets should become L
"أ(ب)ج" // RTL context: brackets should become R
"a(ب)c" // Mixed: brackets take embedding direction

// N1 surrounding strong type test
"a.b" // L NI L → L L L
"أ.ب" // R NI R → R R R
"a.ب" // L NI R → no change (N2 will resolve)

// N2 embedding direction test
"." // Isolated NI → embedding direction
```

## Implementation Notes

### Boundary Neutral (BN) Handling

Per UAX #9 implementation notes, BN characters should be treated specially:
- In N0-N2: Treat BNs that adjoin neutrals the same as those neutrals
- This requires careful handling in bracket pair identification and NI sequence processing

### Performance Considerations

1. **Bracket Pair Stack**: Fixed 63-element stack as per specification
2. **Strong Type Search**: Efficient backward/forward scanning
3. **NI Sequence Identification**: Group consecutive NI characters for batch processing

### Error Handling

1. **Stack Overflow**: BD16 specifies returning empty list if stack overflows
2. **Malformed Brackets**: Handle unmatched brackets gracefully
3. **Invalid Characters**: Robust handling of unexpected character types

## Next Steps

1. Implement core N rules methods in `BidiAlgorithm.cs`
2. Add Unicode bracket property access methods
3. Create comprehensive unit tests
4. Integrate with existing W rules processing pipeline
5. Validate against UAX #9 test cases
