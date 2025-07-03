# BiDi Algorithm I Rules Design Document

## Overview

This document outlines the design and implementation approach for UAX #9 I rules (I1-I2) in the ArbSh BiDi algorithm. The I rules handle the final phase of implicit embedding level resolution, adjusting character embedding levels based on their resolved bidirectional types after W and N rules processing.

## UAX #9 I Rules Specification

### Purpose and Context

The I rules are the final step in resolving embedding levels before reordering. They ensure that:
- Right-to-left text always ends up with an odd level
- Left-to-right and numeric text always end up with an even level  
- Numeric text always ends up with a higher level than the paragraph level
- Characters can reach level max_depth+1 as a result of this process

### I1: Even Level Adjustments

**Rule I1**: For all characters with an even (left-to-right) embedding level, those of type R go up one level and those of type AN or EN go up two levels.

**Logic:**
- Even embedding level (0, 2, 4, 6, ...)
- R characters: level → level + 1 (becomes odd)
- AN characters: level → level + 2 (stays even, but higher)
- EN characters: level → level + 2 (stays even, but higher)
- All other types: level unchanged

### I2: Odd Level Adjustments

**Rule I2**: For all characters with an odd (right-to-left) embedding level, those of type L, EN or AN go up one level.

**Logic:**
- Odd embedding level (1, 3, 5, 7, ...)
- L characters: level → level + 1 (becomes even)
- EN characters: level → level + 1 (becomes even)
- AN characters: level → level + 1 (becomes even)
- All other types: level unchanged

### Summary Table

| Type | Even Level | Odd Level |
|------|------------|-----------|
| **L** | EL | EL+1 |
| **R** | EL+1 | EL |
| **AN** | EL+2 | EL+1 |
| **EN** | EL+2 | EL+1 |

Where EL = current embedding level.

## Current Implementation Analysis

### Existing Code

```csharp
private static void ApplyIRules(BidiCharacterType[] types, int[] levels)
{
    for (int i = 0; i < types.Length; i++)
    {
        var type = types[i];
        var level = levels[i];

        // I1: Even level + (R or AN) character -> next higher odd level
        if (level % 2 == 0 && (type == BidiCharacterType.R || type == BidiCharacterType.AN))
        {
            levels[i] = level + 1;
        }
        // I2: Odd level + (L or EN) character -> next higher even level
        else if (level % 2 == 1 && (type == BidiCharacterType.L || type == BidiCharacterType.EN))
        {
            levels[i] = level + 1;
        }
    }
}
```

### Issues with Current Implementation

1. **I1 Rule Error**: EN characters with even levels should go up by 2 levels, not be ignored
2. **I2 Rule Error**: AN characters with odd levels should go up by 1 level, not be ignored
3. **Missing Level Validation**: No check for max_depth+1 limit
4. **Incomplete Documentation**: Comments don't reflect the full specification

### Correct Implementation Logic

```csharp
private static void ApplyIRules(BidiCharacterType[] types, int[] levels)
{
    for (int i = 0; i < types.Length; i++)
    {
        var type = types[i];
        var level = levels[i];

        if (level % 2 == 0) // Even level (LTR context)
        {
            // I1: R goes up 1 level (even → odd)
            if (type == BidiCharacterType.R)
            {
                levels[i] = level + 1;
            }
            // I1: AN and EN go up 2 levels (even → even, but higher)
            else if (type == BidiCharacterType.AN || type == BidiCharacterType.EN)
            {
                levels[i] = level + 2;
            }
        }
        else // Odd level (RTL context)
        {
            // I2: L, EN, and AN go up 1 level (odd → even)
            if (type == BidiCharacterType.L || 
                type == BidiCharacterType.EN || 
                type == BidiCharacterType.AN)
            {
                levels[i] = level + 1;
            }
        }
    }
}
```

## Detailed Implementation Design

### Core Algorithm Structure

```csharp
/// <summary>
/// Applies UAX #9 I rules (I1-I2) for final embedding level assignment.
/// I1: For characters with even embedding levels:
///     - R characters: level + 1 (even → odd)
///     - AN/EN characters: level + 2 (even → even, higher)
/// I2: For characters with odd embedding levels:
///     - L/EN/AN characters: level + 1 (odd → even)
/// </summary>
private static void ApplyIRules(BidiCharacterType[] types, int[] levels)
{
    for (int i = 0; i < types.Length; i++)
    {
        var type = types[i];
        var currentLevel = levels[i];
        
        // Skip BN characters (Boundary Neutrals) per UAX #9 note
        if (type == BidiCharacterType.BN)
            continue;
            
        int newLevel = ResolveImplicitLevel(type, currentLevel);
        
        // Validate against maximum depth (max_depth + 1 = 126)
        if (newLevel <= MAX_DEPTH + 1)
        {
            levels[i] = newLevel;
        }
        // If exceeding max depth, keep current level (overflow handling)
    }
}

private static int ResolveImplicitLevel(BidiCharacterType type, int currentLevel)
{
    bool isEvenLevel = (currentLevel % 2 == 0);
    
    if (isEvenLevel)
    {
        // I1: Even level adjustments
        switch (type)
        {
            case BidiCharacterType.R:
                return currentLevel + 1; // Even → Odd
            case BidiCharacterType.AN:
            case BidiCharacterType.EN:
                return currentLevel + 2; // Even → Even (higher)
            default:
                return currentLevel; // No change
        }
    }
    else
    {
        // I2: Odd level adjustments
        switch (type)
        {
            case BidiCharacterType.L:
            case BidiCharacterType.EN:
            case BidiCharacterType.AN:
                return currentLevel + 1; // Odd → Even
            default:
                return currentLevel; // No change
        }
    }
}
```

### Integration Points

The I rules are applied after all other type resolution rules:

```csharp
public static List<BidiRun> ProcessRuns(string text, int baseLevel)
{
    // ... X rules, W rules, N rules processing ...
    
    // Phase 5: Apply I rules for final level assignment
    ApplyIRules(types, levels);
    
    // Convert levels array to runs
    runs = ConvertLevelsToRuns(levels);
    
    return runs;
}
```

### Boundary Neutrals (BN) Handling

Per UAX #9 specification: "In rules I1 and I2, ignore BN."

```csharp
// Skip BN characters in I rules processing
if (type == BidiCharacterType.BN)
    continue;
```

### Level Overflow Handling

Characters can reach max_depth+1 (126) as a result of I rules:

```csharp
private const int MAX_DEPTH = 125; // UAX #9 maximum embedding depth

// Validate against maximum depth
if (newLevel <= MAX_DEPTH + 1)
{
    levels[i] = newLevel;
}
// If exceeding max depth, keep current level
```

## Testing Strategy

### Unit Test Cases

1. **I1 Rule Tests:**
   - Even level + R character → level + 1
   - Even level + AN character → level + 2  
   - Even level + EN character → level + 2
   - Even level + L character → no change

2. **I2 Rule Tests:**
   - Odd level + L character → level + 1
   - Odd level + EN character → level + 1
   - Odd level + AN character → level + 1
   - Odd level + R character → no change

3. **Boundary Cases:**
   - BN characters should be ignored
   - Level overflow at max_depth+1
   - Mixed character types in sequence

4. **Integration Tests:**
   - Complete W→N→I rule pipeline
   - Real-world text with mixed scripts
   - Numeric sequences in different contexts

### Test Examples

```csharp
[Fact]
public void ApplyIRules_EvenLevel_RCharacter_IncreasesLevelByOne()
{
    // Even level (0) + R character should become level 1
    var types = new[] { BidiCharacterType.R };
    var levels = new[] { 0 };
    
    ApplyIRules(types, levels);
    
    Assert.Equal(1, levels[0]);
}

[Fact]
public void ApplyIRules_EvenLevel_ENCharacter_IncreasesLevelByTwo()
{
    // Even level (0) + EN character should become level 2
    var types = new[] { BidiCharacterType.EN };
    var levels = new[] { 0 };
    
    ApplyIRules(types, levels);
    
    Assert.Equal(2, levels[0]);
}

[Fact]
public void ApplyIRules_OddLevel_ANCharacter_IncreasesLevelByOne()
{
    // Odd level (1) + AN character should become level 2
    var types = new[] { BidiCharacterType.AN };
    var levels = new[] { 1 };
    
    ApplyIRules(types, levels);
    
    Assert.Equal(2, levels[0]);
}
```

## Implementation Priority

1. **Fix Current Implementation**: Correct the I1 and I2 rule logic
2. **Add BN Handling**: Skip BN characters during processing
3. **Add Level Validation**: Implement max_depth+1 overflow protection
4. **Comprehensive Testing**: Add unit tests for all rule combinations
5. **Integration Verification**: Ensure existing tests continue to pass

## Performance Considerations

- **Single Pass**: Process all characters in one iteration
- **Minimal Branching**: Use efficient conditional logic
- **In-Place Updates**: Modify levels array directly
- **Type Safety**: Use enum comparisons for character types

## Next Steps

1. Update `ApplyIRules` method with correct I1/I2 logic
2. Add BN character handling
3. Implement level overflow protection
4. Add comprehensive unit tests
5. Verify integration with existing W and N rules
6. Update documentation and comments
7. Run full test suite to ensure no regressions
