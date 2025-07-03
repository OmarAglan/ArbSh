# BiDi P Rules (P2-P3) Design Document

## Overview

This document describes the design and implementation of Unicode Bidirectional Algorithm UAX #9 P Rules (P2-P3) for paragraph embedding level determination in the ArbSh project.

## UAX #9 P Rules Specification

### P1 - Paragraph Separation
**P1**: Split the text into separate paragraphs. A paragraph separator (type B) is kept with the previous paragraph. Within each paragraph, apply all the other rules of this algorithm.

*Note: P1 is handled at a higher level in our implementation - each call to the BiDi algorithm processes a single paragraph.*

### P2 - Find First Strong Character
**P2**: In each paragraph, find the first character of type L, AL, or R while skipping over any characters between an isolate initiator and its matching PDI or, if it has no matching PDI, the end of the paragraph.

**Key Requirements:**
- Find first strong character (L, AL, R)
- Skip characters between isolate initiator (LRI, RLI, FSI) and matching PDI
- If isolate has no matching PDI, skip to end of paragraph
- Ignore embedding initiators (LRE, RLE, LRO, RLO) but NOT characters within embeddings
- Characters between isolate initiator and PDI are ignored even if depth limit prevents level raising

### P3 - Set Paragraph Level
**P3**: If a character is found in P2 and it is of type AL or R, then set the paragraph embedding level to one; otherwise, set it to zero.

**Logic:**
- AL or R found → paragraph level = 1 (RTL)
- L found → paragraph level = 0 (LTR)  
- No strong characters found → paragraph level = 0 (LTR default)

## Implementation Design

### Method: `DetermineParagraphLevel`

```csharp
private static int DetermineParagraphLevel(string text, int baseLevel)
```

**Parameters:**
- `text`: The paragraph text to analyze
- `baseLevel`: Explicit level (-1 for auto-detect, 0-1 for explicit)

**Return Value:**
- 0 for LTR paragraph
- 1 for RTL paragraph

### Algorithm Flow

1. **Explicit Level Check**: If `baseLevel` is 0 or 1, return it directly (HL1 override)
2. **P2 Implementation**: Scan text character by character:
   - Convert to Unicode codepoints (handle surrogates)
   - Get character type using `GetCharType()`
   - **Isolate Handling**: If isolate initiator found:
     - Use `FindMatchingPDI()` to find matching PDI
     - If found, skip to position after PDI
     - If not found, break (skip to end of paragraph)
   - **Embedding Handling**: If embedding initiator found, skip character but continue scanning
   - **Strong Character Check**: If L, AL, or R found, apply P3 logic
3. **P3 Implementation**: Return appropriate paragraph level based on first strong character

### Helper Methods

#### `IsEmbeddingInitiator`
```csharp
private static bool IsEmbeddingInitiator(BidiCharacterType charType)
```
Checks for LRE, RLE, LRO, RLO character types.

#### `FindMatchingPDI` (Text Version)
```csharp
private static int FindMatchingPDI(string text, int isolateInitiatorPos)
```
Finds matching PDI for isolate initiator, handling nested isolates with depth tracking.

*Note: Reuses existing `IsIsolateInitiator` method from other BiDi rules.*

## Edge Cases and Special Handling

### 1. Nested Isolates
- Track isolate depth when scanning for matching PDI
- Increment depth for each isolate initiator
- Decrement depth for each PDI
- Match found when depth returns to 0

### 2. Unmatched Isolates
- If no matching PDI found, skip to end of paragraph
- Ensures P2 rule compliance for incomplete isolate sequences

### 3. Mixed Isolates and Embeddings
- Embedding initiators are ignored (skipped) but characters within embeddings are processed
- Isolate content is completely skipped including nested structures

### 4. Surrogate Pairs
- Proper Unicode handling with `char.ConvertToUtf32()`
- Correct position advancement for surrogate pairs

### 5. Empty or Neutral-Only Text
- Default to LTR (paragraph level 0) when no strong characters found
- Handles edge case of text with only weak/neutral characters

## Testing Strategy

### Test Categories

1. **Basic P2/P3 Functionality**
   - Simple LTR and RTL detection
   - Mixed strong character scenarios

2. **Isolate Handling**
   - Single isolates with matching PDI
   - Nested isolates
   - Unmatched isolates
   - Strong characters inside vs outside isolates

3. **Embedding Handling**
   - Embedding initiators ignored
   - Characters within embeddings processed
   - Mixed embeddings and isolates

4. **Edge Cases**
   - Empty text
   - Only neutral characters
   - Only characters inside isolates
   - Complex nested structures

### Test Data Examples

```csharp
// P2: Skip isolate content - LTR first
"a\u2067\u05D0\u2069b" // a + LRI + Hebrew + PDI + b → LTR

// P2: Skip isolate content - RTL first  
"\u05D0\u2067a\u2069\u05D1" // Hebrew + LRI + a + PDI + Hebrew → RTL

// P2: Unmatched isolate
"a\u2067\u05D0b" // a + LRI + Hebrew + b (no PDI) → LTR

// P2: Ignore embedding initiators
"\u202B\u05D0" // RLE + Hebrew → RTL

// P3: Default to LTR
"123 !@#" // Only neutrals → LTR
```

## Integration Points

### Higher-Level Protocol (HL1)
- Explicit `baseLevel` parameter allows HL1 override
- When `baseLevel` is 0 or 1, P2/P3 are bypassed

### BiDi Algorithm Integration
- Called from main `ProcessRuns` method
- Result used as paragraph embedding level for subsequent rules
- Affects base direction for entire paragraph processing

## Performance Considerations

### Optimizations
- Early termination when first strong character found
- Efficient isolate skipping reduces unnecessary character processing
- Reuse of existing helper methods minimizes code duplication

### Complexity
- Time: O(n) where n is text length
- Space: O(1) additional space
- Worst case: Single pass through entire text

## Compliance Verification

### UAX #9 Requirements Met
- ✅ P2: Proper isolate content skipping
- ✅ P2: Embedding initiator ignoring  
- ✅ P2: Nested isolate handling
- ✅ P2: Unmatched isolate handling
- ✅ P3: Correct paragraph level assignment
- ✅ P3: Default LTR behavior

### Standards Compliance
- Full Unicode UAX #9 compliance for P2/P3 rules
- Proper handling of all Unicode bidirectional character types
- Correct isolate and embedding interaction semantics
