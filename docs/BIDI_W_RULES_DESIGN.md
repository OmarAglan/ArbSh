# BiDi W Rules (Weak Types) Implementation Design

## Overview

This document outlines the design and implementation approach for UAX #9 W Rules (W1-W7) for resolving weak character types in the ArbSh BiDi algorithm implementation.

## Background

The W rules resolve weak character types within isolating run sequences. Weak types include:
- **EN** (European Number): European digits, Eastern Arabic-Indic digits
- **ES** (European Number Separator): PLUS SIGN, MINUS SIGN  
- **ET** (European Number Terminator): DEGREE SIGN, currency symbols
- **AN** (Arabic Number): Arabic-Indic digits, Arabic decimal/thousands separators
- **CS** (Common Number Separator): COLON, COMMA, FULL STOP, NO-BREAK SPACE
- **NSM** (Nonspacing Mark): Characters with General_Category Mn/Me
- **BN** (Boundary Neutral): Default ignorables, non-characters, control characters

## W Rules Specification

### W1: Nonspacing Mark Resolution
**Rule**: Examine each NSM in the isolating run sequence:
- If previous character is isolate initiator or PDI → change NSM to ON
- Otherwise → change NSM to type of previous character
- If NSM is at start of sequence → change to type of **sos**

### W2: European Number Context Resolution  
**Rule**: Search backward from each EN until first strong type (R, L, AL, or **sos**):
- If AL found → change EN to AN
- Otherwise → leave EN unchanged

### W3: Arabic Letter Simplification
**Rule**: Change all AL to R

### W4: Number Separator Resolution
**Rule**: 
- Single ES between two EN → change ES to EN
- Single CS between two numbers of same type → change CS to that type

### W5: European Terminator Resolution
**Rule**: Sequence of ET adjacent to EN → change all ET to EN

### W6: Remaining Separator/Terminator Cleanup
**Rule**: All remaining ES, ET, CS (after W4/W5) → change to ON

### W7: European Number Final Resolution
**Rule**: Search backward from each EN until first strong type (R, L, or **sos**):
- If L found → change EN to L
- Otherwise → leave EN unchanged

## Implementation Design

### Core Method Structure
```csharp
private void ApplyWRules(IsolatingRunSequence sequence)
{
    ApplyW1_NonspacingMarks(sequence);
    ApplyW2_EuropeanNumberContext(sequence);
    ApplyW3_ArabicLetterSimplification(sequence);
    ApplyW4_NumberSeparators(sequence);
    ApplyW5_EuropeanTerminators(sequence);
    ApplyW6_RemainingSeparators(sequence);
    ApplyW7_EuropeanNumberFinal(sequence);
}
```

### Data Structures

#### IsolatingRunSequence Class
```csharp
public class IsolatingRunSequence
{
    public List<BidiCharacterType> Types { get; set; }
    public List<int> Positions { get; set; }  // Original text positions
    public BidiCharacterType Sos { get; set; }
    public BidiCharacterType Eos { get; set; }
    public int EmbeddingLevel { get; set; }
}
```

### Individual Rule Implementations

#### W1: Nonspacing Mark Resolution
```csharp
private void ApplyW1_NonspacingMarks(IsolatingRunSequence sequence)
{
    for (int i = 0; i < sequence.Types.Count; i++)
    {
        if (sequence.Types[i] == BidiCharacterType.NSM)
        {
            if (i == 0)
            {
                sequence.Types[i] = sequence.Sos;
            }
            else
            {
                var prevType = sequence.Types[i - 1];
                if (IsIsolateInitiatorOrPDI(prevType))
                {
                    sequence.Types[i] = BidiCharacterType.ON;
                }
                else
                {
                    sequence.Types[i] = prevType;
                }
            }
        }
    }
}
```

#### W2: European Number Context Resolution
```csharp
private void ApplyW2_EuropeanNumberContext(IsolatingRunSequence sequence)
{
    for (int i = 0; i < sequence.Types.Count; i++)
    {
        if (sequence.Types[i] == BidiCharacterType.EN)
        {
            var strongType = SearchBackwardForStrongType(sequence, i);
            if (strongType == BidiCharacterType.AL)
            {
                sequence.Types[i] = BidiCharacterType.AN;
            }
        }
    }
}
```

#### W4: Number Separator Resolution
```csharp
private void ApplyW4_NumberSeparators(IsolatingRunSequence sequence)
{
    for (int i = 1; i < sequence.Types.Count - 1; i++)
    {
        var current = sequence.Types[i];
        var prev = sequence.Types[i - 1];
        var next = sequence.Types[i + 1];
        
        // Single ES between two EN
        if (current == BidiCharacterType.ES && 
            prev == BidiCharacterType.EN && 
            next == BidiCharacterType.EN)
        {
            sequence.Types[i] = BidiCharacterType.EN;
        }
        // Single CS between two numbers of same type
        else if (current == BidiCharacterType.CS &&
                 IsNumberType(prev) && prev == next)
        {
            sequence.Types[i] = prev;
        }
    }
}
```

### Helper Methods

#### Strong Type Search
```csharp
private BidiCharacterType SearchBackwardForStrongType(IsolatingRunSequence sequence, int startIndex)
{
    for (int i = startIndex - 1; i >= 0; i--)
    {
        var type = sequence.Types[i];
        if (IsStrongType(type))
        {
            return type;
        }
    }
    return sequence.Sos;
}

private bool IsStrongType(BidiCharacterType type)
{
    return type == BidiCharacterType.L || 
           type == BidiCharacterType.R || 
           type == BidiCharacterType.AL;
}
```

## Integration with Existing Code

### Modifications to BidiAlgorithm.cs
1. Add `ApplyWRules` method call after X rules processing
2. Implement isolating run sequence construction from level runs
3. Add W rules helper methods
4. Update ProcessRuns method to handle weak type resolution

### Testing Strategy
1. Unit tests for each W rule individually
2. Integration tests with X rules output
3. Test cases covering:
   - NSM resolution in various contexts
   - EN/AN conversion based on AL context
   - Number separator resolution
   - European terminator sequences
   - Mixed weak type scenarios

### Performance Considerations
- Single pass through isolating run sequences
- Efficient backward search with early termination
- Minimal memory allocation for temporary data structures

## Next Steps
1. Implement IsolatingRunSequence class
2. Implement individual W rule methods
3. Add comprehensive unit tests
4. Integrate with existing BidiAlgorithm.ProcessRuns method
5. Verify all existing tests continue to pass
