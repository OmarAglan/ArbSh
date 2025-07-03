# BiDi Algorithm L Rules Design Document

## Overview

This document outlines the design and implementation approach for UAX #9 L rules (L1-L4) in the ArbSh BiDi algorithm. The L rules handle the final reordering phase of the bidirectional algorithm, transforming resolved embedding levels into the correct visual display order.

## UAX #9 L Rules Specification

### L1: Reset Levels for Line Breaks and Separators

**Rule L1**: On each line, reset the embedding level of the following characters to the paragraph embedding level:

1. Segment separators (S)
2. Paragraph separators (B) 
3. Any sequence of whitespace characters and/or isolate formatting characters (FSI, LRI, RLI, PDI) preceding a segment separator or paragraph separator
4. Any sequence of whitespace characters and/or isolate formatting characters (FSI, LRI, RLI, PDI) at the end of the line

**Key Points:**
- Uses *original* character types, not those modified by previous phases
- At most one paragraph separator per line (at the end)
- Ensures trailing whitespace appears at visual end of line in paragraph direction
- Tabulation maintains consistent direction within paragraph

### L2: Reverse Contiguous Sequences by Level

**Rule L2**: From the highest level found in the text to the lowest odd level on each line, including intermediate levels not actually present in the text, reverse any contiguous sequence of characters that are at that level or higher.

**Algorithm:**
1. Find the maximum embedding level in the line
2. For each level from max down to 1 (lowest odd level):
   - Find all contiguous sequences at that level or higher
   - Reverse each sequence
3. Process progressively larger series of substrings

### L3: Combining Marks Reordering

**Rule L3**: Combining marks applied to a right-to-left base character will at this point precede their base character. If the rendering engine expects them to follow the base characters in the final display process, then the ordering of the marks and the base character must be reversed.

**Implementation Note:**
- This is primarily a rendering concern
- May require font-specific adjustments
- Depends on rendering engine expectations

### L4: Character Mirroring

**Rule L4**: A character is depicted by a mirrored glyph if and only if:
- (a) The resolved directionality of that character is R, AND
- (b) The Bidi_Mirrored property value of that character is Yes

**Examples of mirrored characters:**
- Parentheses: ( becomes ), [ becomes ], { becomes }
- Mathematical symbols: < becomes >, ≤ becomes ≥
- Quotation marks and other paired punctuation

## Current Implementation Status

### Existing Implementation

The current `ReorderRunsForDisplay` method in `BidiAlgorithm.cs` implements a basic version of L2:

```csharp
public static string ReorderRunsForDisplay(string originalText, List<BidiRun> runs)
{
    // Find maximum level
    int maxLevel = 0;
    foreach (var run in runs) { if (run.Level > maxLevel) maxLevel = run.Level; }

    // Reverse from highest level down to 1
    for (int level = maxLevel; level >= 1; level--)
    {
        // Reverse runs at this level or higher
        for (int i = 0; i < runs.Count; i++)
        {
            if (runs[i].Level >= level)
            {
                // Find contiguous sequence and reverse
                // ... (basic implementation)
            }
        }
    }
}
```

### Missing Implementation

1. **L1 Rule**: No implementation for resetting levels of separators and trailing whitespace
2. **L2 Rule**: Basic implementation exists but needs refinement for proper contiguous sequence handling
3. **L3 Rule**: No implementation for combining marks reordering
4. **L4 Rule**: No implementation for character mirroring

## Implementation Architecture

### Core Method Structure

```csharp
public static string ReorderRunsForDisplay(string originalText, List<BidiRun> runs, int paragraphLevel)
{
    // L1: Reset levels for separators and trailing whitespace
    ApplyL1_ResetLevels(originalText, runs, paragraphLevel);
    
    // L2: Reverse contiguous sequences by level
    string reorderedText = ApplyL2_ReverseByLevel(originalText, runs);
    
    // L3: Handle combining marks (if needed by rendering engine)
    reorderedText = ApplyL3_CombiningMarks(reorderedText);
    
    // L4: Apply character mirroring
    reorderedText = ApplyL4_CharacterMirroring(reorderedText, runs);
    
    return reorderedText;
}
```

### Data Structures

#### Character Type Cache
```csharp
private struct OriginalCharacterInfo
{
    public BidiCharacterType OriginalType;
    public bool IsBidiMirrored;
    public char MirroredChar;
}
```

#### Level Run Processing
```csharp
private struct LevelRun
{
    public int Start;
    public int Length;
    public int Level;
    public bool IsReversed;
}
```

## Detailed Implementation Plan

### Phase 1: L1 Rule Implementation

**Method**: `ApplyL1_ResetLevels(string text, List<BidiRun> runs, int paragraphLevel)`

1. **Identify Target Characters:**
   - Scan for segment separators (S type)
   - Scan for paragraph separators (B type)
   - Identify whitespace and isolate formatting sequences

2. **Reset Logic:**
   - Reset levels to paragraph embedding level
   - Update BidiRun structures accordingly
   - Merge adjacent runs with same level

3. **Edge Cases:**
   - Handle end-of-line sequences
   - Preserve original character types for identification

### Phase 2: L2 Rule Enhancement

**Method**: `ApplyL2_ReverseByLevel(string text, List<BidiRun> runs)`

1. **Level Analysis:**
   - Find maximum embedding level
   - Identify all levels present (including gaps)

2. **Reversal Algorithm:**
   - For each level from max down to 1:
     - Find contiguous sequences at level or higher
     - Reverse character order within sequences
     - Update position mappings

3. **Optimization:**
   - Use efficient string manipulation
   - Minimize memory allocations
   - Handle Unicode surrogate pairs correctly

### Phase 3: L3 Rule Implementation

**Method**: `ApplyL3_CombiningMarks(string text)`

1. **Mark Detection:**
   - Identify combining marks (NSM type)
   - Find their base characters
   - Determine base character directionality

2. **Reordering Logic:**
   - Check if marks precede RTL base characters
   - Reverse mark-base order if needed
   - Preserve mark attachment relationships

### Phase 4: L4 Rule Implementation

**Method**: `ApplyL4_CharacterMirroring(string text, List<BidiRun> runs)`

1. **Mirroring Conditions:**
   - Check resolved directionality (R)
   - Verify Bidi_Mirrored property
   - Apply character substitution

2. **Character Mapping:**
   - Use ICU4N Bidi_Mirrored property
   - Implement fallback mapping table
   - Handle Unicode normalization

## Integration Points

### With Existing Pipeline

The L rules integrate at the final stage of the BiDi algorithm:

```csharp
public static string ProcessString(string text, int baseLevel)
{
    // Phases 1-5: X, W, N, I rules (already implemented)
    List<BidiRun> runs = ProcessRuns(text, baseLevel);
    
    // Phase 6: L rules (new implementation)
    return ReorderRunsForDisplay(text, runs, baseLevel);
}
```

### Testing Strategy

1. **L1 Tests**: Verify level reset for separators and trailing whitespace
2. **L2 Tests**: Test progressive reversal with complex nesting
3. **L3 Tests**: Validate combining mark reordering
4. **L4 Tests**: Check character mirroring for various scripts
5. **Integration Tests**: End-to-end BiDi processing with real-world text

## Performance Considerations

- **Single Pass Processing**: Minimize text traversals
- **Efficient Reversal**: Use StringBuilder for string manipulation
- **Caching**: Cache character properties and mirroring mappings
- **Memory Management**: Reuse data structures where possible

## Next Steps

1. Implement L1 rule for level reset
2. Enhance L2 rule for proper contiguous sequence reversal
3. Add L3 rule for combining marks (optional, rendering-dependent)
4. Implement L4 rule for character mirroring
5. Add comprehensive unit tests for all L rules
6. Integrate with existing BiDi algorithm pipeline
7. Verify all existing tests continue to pass
8. Add performance benchmarks for reordering operations
