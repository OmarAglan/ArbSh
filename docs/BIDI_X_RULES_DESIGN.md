# UAX #9 X Rules Implementation Design

## Overview

This document outlines the design and implementation approach for UAX #9 X Rules (Explicit Levels and Directions) in the ArbSh BiDi algorithm implementation.

## Background Research

Based on the Unicode Standard Annex #9 (UAX #9), the X rules (X1-X8) handle explicit formatting characters that control bidirectional text embedding and override behavior. These rules process the following character types:

### Explicit Formatting Characters

1. **Embedding Characters:**
   - LRE (U+202A) - Left-to-Right Embedding
   - RLE (U+202B) - Right-to-Left Embedding

2. **Override Characters:**
   - LRO (U+202D) - Left-to-Right Override  
   - RLO (U+202E) - Right-to-Left Override

3. **Isolate Characters:**
   - LRI (U+2066) - Left-to-Right Isolate
   - RLI (U+2067) - Right-to-Left Isolate
   - FSI (U+2068) - First Strong Isolate

4. **Terminating Characters:**
   - PDF (U+202C) - Pop Directional Formatting (terminates LRE/RLE/LRO/RLO)
   - PDI (U+2069) - Pop Directional Isolate (terminates LRI/RLI/FSI)

## Key Design Decisions

### 1. Directional Status Stack

The core data structure for X rules implementation is the directional status stack. Each stack entry contains:

```csharp
private struct DirectionalStatusStackEntry
{
    public int EmbeddingLevel { get; }
    public DirectionalOverrideStatus OverrideStatus { get; }
    public bool IsolateStatus { get; }
}
```

**Design Rationale:**
- `EmbeddingLevel`: Tracks the current embedding depth (0-125)
- `OverrideStatus`: Tracks whether characters should be forced to L or R type
- `IsolateStatus`: Tracks whether the current level was initiated by an isolate

### 2. Maximum Embedding Depth

Following UAX #9 specification:
- Maximum explicit depth: 125 (guaranteed constant in future versions)
- This provides stack overflow protection and consistent results across implementations

### 3. Override Status Enumeration

```csharp
private enum DirectionalOverrideStatus
{
    Neutral,        // No override active
    LeftToRight,    // Force characters to L
    RightToLeft     // Force characters to R
}
```

## X Rules Implementation Plan

### Phase 1: Foundation (Current Implementation)
- ✅ Directional status stack data structure
- ✅ Paragraph level determination (P2, P3)
- ✅ Character type and level initialization
- ✅ Level-to-runs conversion

### Phase 2: Basic Explicit Formatting (Next Task)
Implement X1-X4, X6-X8 rules:

#### X1: Main Processing Loop
- Process each character in the text
- Apply appropriate X2-X8 rules based on character type
- Maintain directional status stack

#### X2: RLE (Right-to-Left Embedding)
- Push current state to stack
- Increment embedding level to next odd value
- Handle stack overflow (max depth 125)

#### X3: LRE (Left-to-Right Embedding)  
- Push current state to stack
- Increment embedding level to next even value
- Handle stack overflow

#### X4: RLO (Right-to-Left Override)
- Push current state to stack
- Set override status to RightToLeft
- Increment embedding level to next odd value

#### X5: LRO (Left-to-Right Override)
- Push current state to stack  
- Set override status to LeftToRight
- Increment embedding level to next even value

#### X6: For all other character types
- Set embedding level based on current stack state
- Apply override if active

#### X7: PDF (Pop Directional Formatting)
- Pop from stack if not empty
- Restore previous embedding level and override status

#### X8: End of paragraph
- Reset stack and levels

### Phase 3: Isolates Support (X5a-X5c, X6a)
- Implement LRI, RLI, FSI processing
- Implement PDI matching and processing
- Handle isolate pairing logic

## Implementation Strategy

### 1. Incremental Development
- Implement basic embedding/override first (X1-X4, X6-X8)
- Add isolates support second (X5a-X5c, X6a)
- Validate each phase with targeted unit tests

### 2. Error Handling
- Stack overflow protection (depth > 125)
- Malformed formatting character sequences
- Unmatched PDF/PDI characters

### 3. Testing Approach
- Unit tests for each X rule individually
- Integration tests with simple embedding scenarios
- Complex nested embedding tests
- Edge cases (stack overflow, malformed sequences)

## Next Steps

1. **Complete Task 1**: Study and Design UAX #9 X Rules ✅
2. **Task 2**: Implement Basic Explicit Formatting (X1-X4, X6-X8)
3. **Task 3**: Implement Isolates Support (X5a-X5c, X6a)
4. **Task 4**: Add comprehensive testing

## References

- Unicode Standard Annex #9: Unicode Bidirectional Algorithm
- UAX #9 Section 3.3.2: Explicit Levels and Directions
- ICU4N library documentation for Unicode character properties

## Version History

- v0.7.7.2: Initial design document and foundation implementation
