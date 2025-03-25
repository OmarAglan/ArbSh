# Arabic Support Developer Guide

This document provides detailed information for developers working on the Arabic language support features of the ArbSh project.

## Overview

The shell's Arabic language support is a critical component that addresses limitations in standard terminals. This guide explains the technical implementation, current capabilities, and guidelines for further development. The implementation focuses on providing robust support for Arabic text in both input and output operations, which is essential for the Baa language ecosystem.

## Technical Implementation

### UTF-8 Character Handling

The shell implements custom UTF-8 character handling rather than relying on standard terminal capabilities:

```c
int get_utf8_char_length(char first_byte);
int read_utf8_char(char *buffer, int max_size);
int utf8_to_codepoint(char *utf8_char, int length);
int codepoint_to_utf8(int codepoint, char *utf8_char);
```

#### Key Implementation Details:

1. **Character Length Detection**: The `get_utf8_char_length` function detects the number of bytes in a UTF-8 character by examining the bit pattern of the first byte.

2. **Character Validation**: The `read_utf8_char` function validates that continuation bytes follow the correct pattern (10xxxxxx).

3. **Codepoint Conversion**: The functions `utf8_to_codepoint` and `codepoint_to_utf8` handle conversion between UTF-8 byte sequences and Unicode codepoints.

### Right-to-Left Text Support

RTL support is implemented in multiple layers:

```c
int is_rtl_char(int unicode_codepoint);
int set_text_direction(int is_rtl);
```

#### Key Implementation Details:

1. **RTL Character Detection**: The `is_rtl_char` function identifies RTL characters by checking Unicode code point ranges (Arabic: 0x0600-0x06FF, Arabic Supplement: 0x0750-0x077F, etc.).

2. **Direction Setting**: The `set_text_direction` function configures the terminal for either RTL or LTR text display using platform-specific mechanisms.

3. **Platform-Specific Implementation**:
   - Windows: Uses console-specific APIs and ANSI escape sequences
   - Unix/Linux: Uses appropriate terminal control sequences

### Terminal Configuration

The shell configures the terminal environment for proper UTF-8 and RTL support:

```c
void configure_terminal_for_utf8(void);
```

#### Key Implementation Details:

1. **Windows Configuration**:
   - Sets console code page to UTF-8 (CP_UTF8, 65001)
   - Enables VT processing for ANSI escape sequences
   - Configures an appropriate font with good Unicode support

2. **Unix/Linux Configuration**:
   - Sets locale to UTF-8
   - Configures terminal settings as needed

### Localization System

The shell includes a complete localization system:

```c
int set_language(int lang_code);
int get_language(void);
const char *get_message(int msg_id);
int detect_system_language(void);
int init_locale(void);
```

#### Key Implementation Details:

1. **Message Catalogs**: Predefined message catalogs for both English and Arabic stored in arrays.

2. **Language Detection**: Detects system language settings via environment variables.

3. **Language Switching**: Allows runtime switching between languages with automatic direction adjustment.

## Arabic Text Handling Considerations

### Bidirectional Text Algorithm

The shell implements a comprehensive bidirectional text algorithm based on the Unicode Bidirectional Algorithm (UAX #9). This implementation handles the complexities of mixed right-to-left and left-to-right text, which is essential for proper Arabic text display.

#### Key Features:

1. **Directional Formatting Characters**: Full support for control characters that affect text direction:
   - LRM/RLM (Left-to-Right/Right-to-Left Marks)
   - LRE/RLE (Left-to-Right/Right-to-Left Embedding)
   - LRO/RLO (Left-to-Right/Right-to-Left Override)
   - PDF (Pop Directional Format)
   - LRI/RLI/FSI (Left-to-Right/Right-to-Left/First Strong Isolate)
   - PDI (Pop Directional Isolate)
   - LRI/RLI/FSI (Left-to-Right/Right-to-Left/First Strong Isolate)
   - PDI (Pop Directional Isolate)

2. **Bidirectional Run Processing**: Text is processed into runs with consistent directional properties:
   ```c
   BidiRun *process_runs(const char *text, int length, int base_level);
   ```

3. **Embedding Level Management**: Proper handling of nested directional contexts with embedding stack:
   ```c
   /* Stack for embedding levels */
   int stack[MAX_DEPTH];
   int stack_top = 0;
   ```

4. **Character Type Classification**: Comprehensive classification of characters according to their bidirectional properties:
   ```c
   static int get_char_type(int codepoint);
   ```

5. **Text Reordering**: Reordering of text segments based on their embedding levels for proper display:
   ```c
   int reorder_runs(BidiRun *runs, const char *text, int length, char *output);
   ```

### Arabic Letter Joining and Text Shaping

Arabic requires proper letter joining behavior, where letters take different forms (initial, medial, final, isolated) depending on their position in words:

1. **Current Implementation**: 
   - Basic joining handled via platform rendering
   - Character position analysis to determine joining forms
   - Support for special ligatures like Lam-Alef (لا)
   - Proper handling of non-joining characters

2. **Text Shaping Algorithm**:
   - Character classification (joining vs. non-joining)
   - Context analysis for determining character form
   - Special case handling for mandatory ligatures
   - Fallback mechanisms for platforms with limited rendering capabilities

3. **Implementation Details**:
   ```c
   /* Determine Arabic letter form based on context */
   int get_arabic_letter_form(int codepoint, int prev_char, int next_char);
   
   /* Apply Arabic text shaping to a string */
   int shape_arabic_text(const char *input, int length, char *output);
   ```

### Arabic Numbers and Punctuation

Arabic text may use either Arabic-Indic digits (٠١٢٣٤٥٦٧٨٩) or Western Arabic digits (0123456789):

1. **Current Status**: Both digit types supported
2. **Needed Improvements**: Number formatting according to locale (thousands separators, decimal points)

## Windows-Specific Implementation

Windows requires special handling for proper Arabic support:

### Console Configuration

```c
/* Windows console configuration */
SetConsoleOutputCP(65001); /* CP_UTF8 */
SetConsoleCP(65001);       /* CP_UTF8 */
```

### Font Selection

The shell selects fonts with good Unicode and Arabic support:

```c
/* Configure console font */
CONSOLE_FONT_INFOEX cfi;
cfi.cbSize = sizeof(cfi);
// ...
wcscpy(cfi.FaceName, L"Consolas"); /* Use a font with good Unicode support */
SetCurrentConsoleFontEx(hOut, FALSE, &cfi);
```

Consider also supporting these fonts which have excellent Arabic support:
- Cascadia Code (preferred for modern Windows)
- Courier New (widely available)
- Arabic Typesetting (specialized for Arabic)

### ANSI Escape Sequences

Windows 10+ supports ANSI escape sequences when properly enabled:

```c
/* Enable VT processing for ANSI escape sequences */
DWORD dwMode = 0;
GetConsoleMode(hOut, &dwMode);
dwMode |= 0x0004; /* ENABLE_VIRTUAL_TERMINAL_PROCESSING */
SetConsoleMode(hOut, dwMode);
```

## Testing Arabic Support

### Test Strings

Use these test strings to verify correct handling:

#### Basic Arabic Text:
```
مرحبا بالعالم
```

#### Mixed Directional Text:
```
هذا النص يحتوي English words في وسطه
```

#### Arabic with Numbers:
```
العدد ١٢٣٤٥ والعدد 67890
```

#### Text with Diacritics:
```
العَرَبِيَّة مَعَ تَشْكِيل كَامِل
```

### Test Process

1. **Input Testing**: Verify that Arabic text can be entered correctly
2. **Output Testing**: Verify that Arabic text is displayed properly
3. **Cursor Movement**: Test cursor navigation through Arabic text
4. **Text Selection**: Test selection of Arabic text
5. **Copy/Paste**: Test copy and paste of Arabic text
6. **Command History**: Verify that Arabic commands are properly stored and recalled

## Performance Considerations

Arabic text processing can be more resource-intensive than Latin text:

1. **Character Processing Overhead**: Multi-byte characters require more processing
2. **Bidirectional Resolution**: Bidirectional algorithm adds complexity
3. **Rendering Complexity**: Letter shaping and joining add rendering overhead

### Optimization Approaches:

1. **Buffered Processing**: Process text in chunks rather than character-by-character
2. **Caching**: Cache directional properties of frequently used text
3. **Avoid Redundant Processing**: Only re-process text when necessary

## Future Work

Priority areas for improving Arabic support:

1. **Complete Bidirectional Algorithm**: Implement full Unicode Bidirectional Algorithm
2. **Enhanced Text Editor**: More sophisticated text editing with proper Arabic support
3. **Input Method Enhancement**: Better keyboard layout and input method support
4. **Text Shaping**: Improved letter shaping and ligature handling
5. **Right-to-Left UI Elements**: Adapt UI for RTL orientation when using Arabic

## Resources

### Unicode Standards
- [Unicode Bidirectional Algorithm](https://unicode.org/reports/tr9/)
- [Unicode Arabic Presentation Forms](https://unicode.org/charts/PDF/UFB50.pdf)

### Testing Tools
- [Bidirectional Checker](https://unicode.org/cldr/utility/bidi.jsp)
- [Arabic Shaping Tester](https://www.w3.org/International/tests/repo/results/arabic-shaping)

### Libraries
- [ICU (International Components for Unicode)](http://site.icu-project.org/)
- [FriBidi](https://github.com/fribidi/fribidi) - Implementation of the Unicode Bidirectional Algorithm

## Conclusion

The Arabic support in our shell is a critical component for the Baa language ecosystem. By following this guide and continuing to enhance the implementation, developers can ensure that the shell provides a robust environment for Arabic text processing, essential for the Baa language's success.
