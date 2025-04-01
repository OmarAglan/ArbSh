# Arabic Support Developer Guide

This document provides detailed information for developers working on the Arabic language support features of the ArbSh project.

## Overview

The shell's Arabic language support is a critical component addressing limitations in standard terminals. This guide explains the technical implementation of UTF-8 handling, Right-to-Left (RTL) support, the Bidirectional Algorithm, localization, and platform considerations. This support is essential for the Baa language ecosystem.

## Technical Implementation

### UTF-8 Character Handling (`src/utils/utf8.c`)

Custom UTF-8 handling ensures correct processing independent of terminal capabilities:

- `int get_utf8_char_length(char first_byte)`: Detects byte length (1-4) of a UTF-8 character from its first byte.
- `int read_utf8_char(char *buffer, int max_size)`: Reads and validates a full UTF-8 character from a buffer.
- `int utf8_to_codepoint(const char *utf8_char, int *codepoint)`: Converts a validated UTF-8 sequence to its Unicode codepoint.
- `int codepoint_to_utf8(int codepoint, char *utf8_char)`: Converts a Unicode codepoint back to its UTF-8 sequence.

### Right-to-Left (RTL) Text Support

RTL support is managed through character detection and terminal configuration:

- `int is_rtl_char(int unicode_codepoint)`: Identifies RTL characters (Arabic, Hebrew ranges) by Unicode codepoint.
- `int set_text_direction(int is_rtl)`: Configures the *console* terminal for RTL or LTR display using platform-specific VT escape sequences (e.g., `\xE2\x80\x8F` for RTL mark, `\033]8;;bidi=R\a`).

### Terminal Configuration (`src/utils/utf8.c`)

The `configure_terminal_for_utf8()` function sets up the terminal environment:

- **Windows:**
  - Sets console input/output code pages to UTF-8 (65001).
  - Enables `ENABLE_VIRTUAL_TERMINAL_PROCESSING` for ANSI/VT escape sequence support.
  - Attempts to set a console font with good Unicode/Arabic support (e.g., Consolas, Arial, Tahoma).
- **Unix/Linux:**
  - Relies on standard locale settings (e.g., `en_US.UTF-8`) being configured externally. ArbSh itself calls `setlocale(LC_ALL, "en_US.UTF-8")` as a fallback/default.

### Localization System (`src/i18n/locale/locale.c`)

A simple localization system provides English and Arabic messages:

- `int set_language(int lang_code)`: Switches the active language (LANG_EN=0, LANG_AR=1) and calls `set_text_direction`.
- `int get_language(void)`: Returns the current language code.
- `const char *get_message(int msg_id)`: Retrieves a message string based on ID and current language.
- `int detect_system_language(void)`: Basic detection via `LANG` environment variable.
- `int init_locale(void)`: Initializes locale, detects language, configures terminal.

### Arabic Input (`src/i18n/arabic_input.c`)

Handles keyboard layout switching:

- `int toggle_arabic_mode(void)`: Toggles the internal Arabic mode flag.
- `int is_arabic_mode(void)`: Checks the flag.
- `int set_keyboard_layout(int layout)`: Sets internal layout state (0=EN, 1=AR).
- `int get_keyboard_layout(void)`: Gets the state.
- `int _mylayout(info_t *info)`: Built-in command (`layout ar|en|toggle`) to change layout.
- *Note:* Actual keyboard mapping relies on the OS keyboard layout settings. This module primarily tracks the *intended* layout state for the shell.

## Arabic Text Handling Considerations

### Bidirectional Text Algorithm (`src/i18n/bidi/bidi.c`)

ArbSh implements a comprehensive bidirectional text algorithm based on the **Unicode Bidirectional Algorithm (UAX #9)**. This handles the complexities of displaying mixed right-to-left (e.g., Arabic, Hebrew) and left-to-right (e.g., English, numbers) text.

**Key Implementation Features:**

1. **UAX #9 Compliance:** Aims to follow the rules specified in the Unicode standard.
2. **Character Type Classification:** `get_char_type(int codepoint)` classifies characters based on their BiDi properties (L, R, AL, EN, AN, WS, ON, B, S, ET, ES, CS, NSM, and directional formatting characters).
3. **Directional Formatting Characters:** Supports explicit control characters:
    - Marks: LRM (U+200E), RLM (U+200F)
    - Embeddings: LRE (U+202A), RLE (U+202B), PDF (U+202C)
    - Overrides: LRO (U+202D), RLO (U+202E), PDF (U+202C)
    - Isolates: LRI (U+2066), RLI (U+2067), FSI (U+2068), PDI (U+2069)
4. **Run Processing:** `process_runs(const char *text, int length, int base_level)` segments the text into runs of characters with the same embedding level. It manages an embedding level stack (`stack[MAX_DEPTH]`) to handle nested directional contexts.
5. **Level Resolution:** Implicitly resolves embedding levels based on character types and explicit formatting codes according to UAX #9 rules within `process_runs`.
6. **Text Reordering:** `reorder_runs(BidiRun *runs, const char *text, int length, char *output)` reorders the runs based on their resolved embedding levels for correct visual display. RTL runs have their characters reversed internally during this process. Directional formatting characters are typically removed during reordering.
7. **Main Function:** `process_bidirectional_text(const char *text, int length, int is_rtl, char *output)` orchestrates the process: calls `process_runs`, then `reorder_runs`.

This implementation allows ArbSh to handle complex strings like `"Hello مرحبا (World) بالعالم 123"` correctly, displaying each segment in its proper direction.

### Arabic Letter Joining and Text Shaping

**Current Status:** ArbSh **does not** implement its own Arabic text shaping (letter joining, ligatures) logic.

- **Reliance on Renderer:** It relies entirely on the underlying rendering engine to perform text shaping:
  - **Console Mode:** Depends on the capabilities of the terminal emulator and the selected console font. Modern terminals (like Windows Terminal, gnome-terminal) with appropriate fonts handle shaping well.
  - **ImGui GUI Mode:** Depends on the font rendering capabilities used by ImGui (typically FreeType) and the loaded font file containing the necessary OpenType shaping tables (GSUB, GPOS).
- **Future Work (Optional):** Implementing shaping within ArbSh (e.g., using HarfBuzz) would provide consistent shaping independent of the renderer but adds significant complexity and dependencies. The functions `get_arabic_letter_form` and `shape_arabic_text` mentioned previously are *not* currently implemented.

### Arabic Numbers and Punctuation

- Both Western (0-9) and Arabic-Indic (٠-٩) digits are handled correctly by the UTF-8 and BiDi algorithms.
- Display and context rules (e.g., whether numbers within Arabic text flow RTL or LTR) are determined by the BiDi algorithm (`get_char_type` classifies them as EN or AN).
- Punctuation handling also follows BiDi rules.

## Windows-Specific Implementation

Windows requires special handling:

- **Console Configuration:** `configure_terminal_for_utf8` uses `SetConsoleOutputCP`, `SetConsoleCP`, `SetConsoleMode` (for VT processing), and `SetCurrentConsoleFontEx`.
- **Font Selection:** Attempts to select fonts like Consolas, Arial, Tahoma known for good Unicode/Arabic support.
- **GUI Mode:** Uses ImGui with DirectX, relying on ImGui's font rendering (which often uses FreeType) for shaping.

## Testing Arabic Support (`tests/arabic/`)

Specific tests verify Arabic functionality:

- `test_utf8.c`: Tests UTF-8 encoding/decoding and RTL character detection.
- `test_bidi.c`: Tests the `process_bidirectional_text` function with various mixed strings.
- `test_keyboard.c`: Tests the logic related to the `layout` command and internal state tracking (not actual OS keyboard switching).

Use complex test strings including mixed directions, numbers, punctuation, and formatting codes to validate the BiDi implementation.

## Performance Considerations

- The BiDi algorithm (`bidi.c`) involves character-by-character analysis and run processing, which can add overhead compared to simple LTR text.
- Reordering text (`reorder_runs`) involves memory manipulation.
- Performance impact is most noticeable on very long lines or large blocks of text processed at once.
- Optimization strategies could include caching BiDi properties or processing text incrementally.

## Conclusion

ArbSh provides a strong foundation for Arabic language support through custom UTF-8 handling and a comprehensive implementation of the Unicode Bidirectional Algorithm. While text shaping relies on the rendering engine, the core logic ensures correct directional handling essential for the Baa language ecosystem.
