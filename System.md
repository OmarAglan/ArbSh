---
description: ArbSh Development Workflow - Feature Development Loop
---

# ArbSh Development Workflow

This workflow defines the standard process for developing new features in the ArbSh (Arabic-First Shell) project.

## ⚠️ CRITICAL: Approval Required

**STOP AND WAIT FOR USER APPROVAL** at each step marked with 🛑

Do NOT proceed to the next step without explicit user confirmation (e.g., "ok", "proceed", "next", "continue").

---

## Core Principles

1. **Arabic-First Documentation**: All code comments and XML documentation must be in Arabic (or bilingual where appropriate).
2. **BiDi Compliance**: Every feature must respect Unicode Bidirectional Algorithm (UAX #9).
3. **Full File Output**: When providing code, output the complete file contents.
4. **Preserve Existing Features**: Never break or remove existing functionality.
5. **Iterative Approval**: Each feature goes through planning → approval → implementation.
6. **Changelog Updates**: Every change must be logged in CHANGELOG.md.
7. **Wait for Approval**: NEVER proceed to next step without user confirmation.

---

## Development Loop

### Step 1: Feature Planning 📋 🛑

1. Review the current [ROADMAP.md](ROADMAP.md) to identify the next planned feature.
2. Create a detailed feature specification including:
   - Feature name (in Arabic and English).
   - Description and purpose.
   - Syntax examples (for shell commands).
   - Implementation approach (Pipeline, Cmdlet, or Core Logic).
   - New files to be created (e.g., `NewCmdlet.cs`).
3. Document the plan in an `implementation_plan.md` artifact.
4. **🛑 STOP: Present plan and WAIT for user approval before Step 2**

### Step 2: Documentation Updates 📖 🛑

Before writing any code:
1. Update [docs/USAGE_EXAMPLES.md](docs/USAGE_EXAMPLES.md) with new command usage (if applicable).
2. Update [docs/PROJECT_ORGANIZATION.md](docs/PROJECT_ORGANIZATION.md) if architecture changes.
3. Update [docs/BIDI_RULES_DESIGN.md](docs/) if modifying BiDi algorithms.
4. **🛑 STOP: Show changes and WAIT for approval**

### Step 3: Architecture & Context (Models/Interfaces) 🏗️ 

1. Modify/Create files in `src_csharp/ArbSh.Console/Models` or `Interfaces`:
   - Define new data structures or pipeline objects.
   - Add new Attributes (e.g., `[ArabicName]`).
   - All comments in Arabic (XML docs).
2. **Provide FULL FILE contents**.
3. **🛑 STOP: Show Model/Interface changes and WAIT for approval**

### Step 4: Core Logic (Parsing/I18n) 🧠 

1. Modify `src_csharp/ArbSh.Console/Parsing` or `I18n`:
   - Add tokenizer rules for new syntax.
   - Update BiDi algorithm rules if necessary.
   - Arabic comments explaining logic.
2. **Provide FULL FILE contents**.
3. **🛑 STOP: Show Core Logic changes and WAIT for approval**

### Step 5: Implementation (Cmdlets/Program) 🔧 🛑

1. Modify/Create files in `src_csharp/ArbSh.Console/Commands`:
   - Implement the `Cmdlet` logic.
   - Ensure `[ArabicName]` and parameters are defined.
   - Handle Pipeline input/output.
2. **Provide FULL FILE contents**.
3. **🛑 STOP: Show Cmdlet changes and WAIT for approval**

### Step 6: Test Case Generation 🧪 🛑

1. Create or Update tests in `src_csharp/ArbSh.Test`:
   - Add Unit Tests for new logic.
   - Add BiDi rendering tests if applicable.
   - Ensures high code coverage.
2. **Provide FULL FILE contents**.
3. **🛑 STOP: Show Test file changes and WAIT for approval before running**

### Step 7: Build and Test 🔨 🛑

```powershell
# Navigate to solution root
cd d:\My Dev Life\Software Dev\ArbSh\src_csharp

# Build the solution
dotnet build

# Run Tests
dotnet test

# Run the Shell manually for verification
dotnet run --project ArbSh.Console
# (Manual steps: Try the new command/feature in the shell)
```

1. Run build and test commands.
2. Report results.
3. **🛑 STOP: Show test results and WAIT for approval before updating docs**

### Step 8: Update CHANGELOG.md 📝 🛑

Add entry under `[Unreleased]` section:

```markdown
## [Unreleased]

### Added
- **Feature Name** — Description of what was added

### Changed
- Description of behavior changes

### Fixed
- Description of bug fixes
```

1. **🛑 STOP: Show CHANGELOG.md changes and WAIT for approval**

### Step 9: Update ROADMAP.md ✅ 🛑

Mark the completed feature:
- Change `[ ]` to `[x]` for completed items.
- Check if "Phase" is complete.

1. **🛑 STOP: Show ROADMAP.md changes and WAIT for approval**

### Step 10: Loop to Next Feature 🔄

1. Ask user which feature to work on next.
2. **🛑 STOP: WAIT for user to specify next feature**.
3. Return to Step 1.

---

## Approval Phrases

The following user responses mean "proceed":
- "ok" / "OK" / "Ok"
- "proceed"
- "next"
- "continue"
- "yes"
- "go ahead"
- "approved"
- "looks good"
- "lgtm"
- "👍"

Any other response should be treated as feedback requiring changes.

---

## Code Style Requirements

### Arabic XML Documentation

```csharp
/// <summary>
/// يمثل هذا الصنف الأمر الأساسي للطباعة.
/// Represents the basic print command.
/// </summary>
[ArabicName("اطبع")]
public class WriteOutputCommand : Cmdlet
{
    /// <summary>
    /// النص المراد طباعته.
    /// The text to print.
    /// </summary>
    [Parameter(Position = 0)]
    [ArabicName("النص")]
    public string Text { get; set; }
}
```

### Inline Comments

```csharp
// التحقق من اتجاه النص
// Check text direction
if (BidiHelper.IsRtl(text)) {
    // معالجة النصوص العربية
    // Handle Arabic text
    ProcessRtl(text);
}
```

### File Modification Checklist

When modifying any source file, ensure:

- [ ] All new code has Arabic XML documentation.
- [ ] `[ArabicName]` attributes are used for user-facing symbols.
- [ ] Existing functionality (Pipeline, BiDi) is preserved.
- [ ] Code compiles without warnings (`dotnet build`).
- [ ] Tests passed (`dotnet test`).
- [ ] CHANGELOG.md updated.
- [ ] ROADMAP.md updated.
- [ ] **User approved each file before moving to next**.
