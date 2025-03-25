#include "shell.h"

/**
 * init_bidi - Initialize the bidirectional text support
 *
 * This function initializes any resources needed for bidirectional text support.
 */
void init_bidi(void)
{
    /* No initialization needed at this time */
}

/* Bidirectional character types as per Unicode Bidirectional Algorithm (UAX #9) */
#define BIDI_TYPE_L 0    /* Left-to-Right */
#define BIDI_TYPE_R 1    /* Right-to-Left */
#define BIDI_TYPE_EN 2   /* European Number */
#define BIDI_TYPE_ES 3   /* European Number Separator */
#define BIDI_TYPE_ET 4   /* European Number Terminator */
#define BIDI_TYPE_AN 5   /* Arabic Number */
#define BIDI_TYPE_CS 6   /* Common Number Separator */
#define BIDI_TYPE_B 7    /* Paragraph Separator */
#define BIDI_TYPE_S 8    /* Segment Separator */
#define BIDI_TYPE_WS 9   /* Whitespace */
#define BIDI_TYPE_ON 10  /* Other Neutral */
#define BIDI_TYPE_NSM 11 /* Non-spacing Mark */
#define BIDI_TYPE_AL 12  /* Arabic Letter */
#define BIDI_TYPE_LRE 13 /* Left-to-Right Embedding */
#define BIDI_TYPE_RLE 14 /* Right-to-Left Embedding */
#define BIDI_TYPE_PDF 15 /* Pop Directional Format */
#define BIDI_TYPE_LRO 16 /* Left-to-Right Override */
#define BIDI_TYPE_RLO 17 /* Right-to-Left Override */
#define BIDI_TYPE_LRI 18 /* Left-to-Right Isolate */
#define BIDI_TYPE_RLI 19 /* Right-to-Left Isolate */
#define BIDI_TYPE_FSI 20 /* First Strong Isolate */
#define BIDI_TYPE_PDI 21 /* Pop Directional Isolate */
#define BIDI_TYPE_LRM 22 /* Left-to-Right Mark */
#define BIDI_TYPE_RLM 23 /* Right-to-Left Mark */

/* Maximum number of embedding levels */
#define MAX_DEPTH 125

/**
 * BidiRun - Structure to hold a sequence of characters with the same embedding level
 */
typedef struct _BidiRun
{
    int start;  /* Start position in text */
    int length; /* Length of the run */
    int level;  /* Embedding level */
    struct _BidiRun *next;
} BidiRun;

/**
 * get_char_type - Determines the bidirectional character type
 * @codepoint: Unicode codepoint to check
 *
 * Return: Bidirectional character type
 */
int get_char_type(int codepoint)
{
    /* Arabic Letters */
    if ((codepoint >= 0x0600 && codepoint <= 0x06FF) ||
        (codepoint >= 0x0750 && codepoint <= 0x077F) ||
        (codepoint >= 0x08A0 && codepoint <= 0x08FF))
        return BIDI_TYPE_AL;

    /* Hebrew Letters */
    if (codepoint >= 0x0590 && codepoint <= 0x05FF)
        return BIDI_TYPE_R;

    /* Numbers */
    if (codepoint >= 0x0030 && codepoint <= 0x0039)
        return BIDI_TYPE_EN;

    /* Arabic Numbers */
    if ((codepoint >= 0x0660 && codepoint <= 0x0669) ||   /* Arabic-Indic digits */
        (codepoint >= 0x06F0 && codepoint <= 0x06F9))     /* Extended Arabic-Indic digits */
        return BIDI_TYPE_AN;

    /* Directional Formatting Characters */
    switch (codepoint)
    {
    case 0x200E:
        return BIDI_TYPE_LRM; /* LEFT-TO-RIGHT MARK */
    case 0x200F:
        return BIDI_TYPE_RLM; /* RIGHT-TO-LEFT MARK */
    case 0x202A:
        return BIDI_TYPE_LRE; /* LEFT-TO-RIGHT EMBEDDING */
    case 0x202B:
        return BIDI_TYPE_RLE; /* RIGHT-TO-LEFT EMBEDDING */
    case 0x202C:
        return BIDI_TYPE_PDF; /* POP DIRECTIONAL FORMATTING */
    case 0x202D:
        return BIDI_TYPE_LRO; /* LEFT-TO-RIGHT OVERRIDE */
    case 0x202E:
        return BIDI_TYPE_RLO; /* RIGHT-TO-LEFT OVERRIDE */
    case 0x2066:
        return BIDI_TYPE_LRI; /* LEFT-TO-RIGHT ISOLATE */
    case 0x2067:
        return BIDI_TYPE_RLI; /* RIGHT-TO-LEFT ISOLATE */
    case 0x2068:
        return BIDI_TYPE_FSI; /* FIRST STRONG ISOLATE */
    case 0x2069:
        return BIDI_TYPE_PDI; /* POP DIRECTIONAL ISOLATE */
    }

    /* Whitespace */
    if (codepoint == 0x0020 || codepoint == 0x0009 || codepoint == 0x00A0)
        return BIDI_TYPE_WS;

    /* Paragraph Separator */
    if (codepoint == 0x000A || codepoint == 0x000D || codepoint == 0x2029)
        return BIDI_TYPE_B;

    /* Segment Separator */
    if (codepoint == 0x0009 || codepoint == 0x001F)
        return BIDI_TYPE_S;

    /* Default to LTR for ASCII range */
    if (codepoint < 0x0080)
        return BIDI_TYPE_L;

    /* Default to ON for everything else */
    return BIDI_TYPE_ON;
}

/**
 * create_bidi_run - Creates a new bidirectional run
 * @start: Start position
 * @length: Length of the run
 * @level: Embedding level
 *
 * Return: Pointer to new BidiRun or NULL on failure
 */
static BidiRun *create_bidi_run(int start, int length, int level)
{
    BidiRun *run = malloc(sizeof(BidiRun));
    if (!run)
        return NULL;

    run->start = start;
    run->length = length;
    run->level = level;
    run->next = NULL;
    return run;
}

/**
 * process_runs - Process text into bidirectional runs
 * @text: Input text buffer
 * @length: Length of text
 * @base_level: Base paragraph level (0 for LTR, 1 for RTL)
 *
 * Return: Linked list of BidiRun structures
 */
BidiRun *process_runs(const char *text, int length, int base_level)
{
    BidiRun *runs = NULL;
    BidiRun *last_run = NULL;
    int current_level = base_level;
    int run_start = 0;
    int i = 0;

    /* Stack for embedding levels */
    int stack[MAX_DEPTH];
    int stack_top = 0;
    stack[0] = base_level;

    /* Directional status */
    int override_status = -1; /* -1: neutral, 0: LTR, 1: RTL */
    int isolate_status = 0;   /* 0: not in isolate, 1: in isolate */

    while (i < length)
    {
        int char_len = get_utf8_char_length(text[i]);
        if (char_len == 0)
            break;

        /* Convert UTF-8 to Unicode codepoint */
        char utf8_buf[4];
        memcpy(utf8_buf, text + i, char_len);
        int codepoint;
        if (!utf8_to_codepoint(utf8_buf, &codepoint))
            break;

        /* Get character type */
        int char_type = get_char_type(codepoint);

        /* Handle directional formatting characters */
        int new_level = current_level;
        int create_new_run = 0;

        switch (char_type)
        {
        case BIDI_TYPE_LRE: /* Left-to-Right Embedding */
            if (stack_top < MAX_DEPTH - 1)
            {
                stack_top++;
                stack[stack_top] = (current_level + 2) & ~1; /* Ensure even level (LTR) */
                current_level = stack[stack_top];
                create_new_run = 1;
            }
            break;

        case BIDI_TYPE_RLE: /* Right-to-Left Embedding */
            if (stack_top < MAX_DEPTH - 1)
            {
                stack_top++;
                stack[stack_top] = (current_level + 1) | 1; /* Ensure odd level (RTL) */
                current_level = stack[stack_top];
                create_new_run = 1;
            }
            break;

        case BIDI_TYPE_PDF: /* Pop Directional Format */
            if (stack_top > 0)
            {
                stack_top--;
                current_level = stack[stack_top];
                create_new_run = 1;
            }
            break;

        case BIDI_TYPE_LRO: /* Left-to-Right Override */
            if (stack_top < MAX_DEPTH - 1)
            {
                stack_top++;
                stack[stack_top] = (current_level + 2) & ~1; /* Ensure even level (LTR) */
                current_level = stack[stack_top];
                override_status = 0; /* LTR override */
                create_new_run = 1;
            }
            break;

        case BIDI_TYPE_RLO: /* Right-to-Left Override */
            if (stack_top < MAX_DEPTH - 1)
            {
                stack_top++;
                stack[stack_top] = (current_level + 1) | 1; /* Ensure odd level (RTL) */
                current_level = stack[stack_top];
                override_status = 1; /* RTL override */
                create_new_run = 1;
            }
            break;

        case BIDI_TYPE_LRI: /* Left-to-Right Isolate */
            if (stack_top < MAX_DEPTH - 1)
            {
                stack_top++;
                stack[stack_top] = (current_level + 2) & ~1; /* Ensure even level (LTR) */
                current_level = stack[stack_top];
                isolate_status = 1;
                create_new_run = 1;
            }
            break;

        case BIDI_TYPE_RLI: /* Right-to-Left Isolate */
            if (stack_top < MAX_DEPTH - 1)
            {
                stack_top++;
                stack[stack_top] = (current_level + 1) | 1; /* Ensure odd level (RTL) */
                current_level = stack[stack_top];
                isolate_status = 1;
                create_new_run = 1;
            }
            break;

        case BIDI_TYPE_FSI: /* First Strong Isolate */
            /* For simplicity, treat as RLI for now */
            if (stack_top < MAX_DEPTH - 1)
            {
                stack_top++;
                stack[stack_top] = (current_level + 1) | 1; /* Ensure odd level (RTL) */
                current_level = stack[stack_top];
                isolate_status = 1;
                create_new_run = 1;
            }
            break;

        case BIDI_TYPE_PDI: /* Pop Directional Isolate */
            if (isolate_status && stack_top > 0)
            {
                stack_top--;
                current_level = stack[stack_top];
                isolate_status = 0;
                create_new_run = 1;
            }
            break;

        default:
            /* Apply character type based on overrides */
            if (override_status == 0)
            {
                /* LTR override */
                char_type = BIDI_TYPE_L;
            }
            else if (override_status == 1)
            {
                /* RTL override */
                char_type = BIDI_TYPE_R;
            }

            /* Determine if we need to start a new run based on character type */
            if (char_type == BIDI_TYPE_AL || char_type == BIDI_TYPE_R)
                new_level = (current_level + 1) | 1; /* Ensure odd level (RTL) */
            else if (char_type == BIDI_TYPE_L)
                new_level = (current_level + 2) & ~1; /* Ensure even level (LTR) */

            if (new_level != current_level)
            {
                create_new_run = 1;
            }
            break;
        }

        /* Create a new run if needed */
        if (create_new_run)
        {
            /* Create a run for the previous segment */
            if (i > run_start)
            {
                BidiRun *run = create_bidi_run(run_start, i - run_start, current_level);
                if (!run)
                    goto cleanup;

                if (!runs)
                    runs = run;
                else
                    last_run->next = run;
                last_run = run;
            }

            run_start = i;
            current_level = new_level;
        }

        i += char_len;
    }

    /* Create the final run */
    if (i > run_start)
    {
        BidiRun *run = create_bidi_run(run_start, i - run_start, current_level);
        if (!run)
            goto cleanup;

        if (!runs)
            runs = run;
        else
            last_run->next = run;
    }

    return runs;

cleanup:
    /* Free all allocated runs on error */
    while (runs)
    {
        BidiRun *next = runs->next;
        free(runs);
        runs = next;
    }
    return NULL;
}

/**
 * reorder_runs - Reorders bidirectional runs for display
 * @runs: Linked list of BidiRun structures
 * @text: Original text buffer
 * @length: Length of text
 * @output: Output buffer for reordered text
 *
 * Return: Length of reordered text
 */
int reorder_runs(BidiRun *runs, const char *text, int length, char *output)
{
    BidiRun *run;
    int out_pos = 0;
    int max_level = 0;
    
    /* Find maximum embedding level */
    for (run = runs; run != NULL; run = run->next)
    {
        if (run->level > max_level)
            max_level = run->level;
    }
    
    /* Handle unused parameter to avoid compiler warning */
    (void)length;
    
    /* Process levels from highest to lowest */
    for (int level = max_level; level >= 0; level--)
    {
        /* Find runs at this level */
        for (run = runs; run; run = run->next)
        {
            if (run->level == level)
            {
                /* Copy run text to output */
                if (level % 2)
                { /* RTL */
                    /* Copy characters in reverse order */
                    int pos = run->start + run->length;
                    while (pos > run->start)
                    {
                        int prev_char_len = 1;
                        /* Find the start of the UTF-8 character */
                        while (prev_char_len <= 4 && pos - prev_char_len >= run->start &&
                               (text[pos - prev_char_len] & 0xC0) == 0x80)
                            prev_char_len++;

                        pos -= prev_char_len;

                        /* Skip directional formatting characters */
                        char utf8_buf[4];
                        memcpy(utf8_buf, text + pos, prev_char_len);
                        int codepoint;
                        if (!utf8_to_codepoint(utf8_buf, &codepoint))
                            continue;
                        int char_type = get_char_type(codepoint);

                        /* Skip directional formatting characters in output */
                        if (char_type != BIDI_TYPE_LRE && char_type != BIDI_TYPE_RLE &&
                            char_type != BIDI_TYPE_PDF && char_type != BIDI_TYPE_LRO &&
                            char_type != BIDI_TYPE_RLO && char_type != BIDI_TYPE_LRI &&
                            char_type != BIDI_TYPE_RLI && char_type != BIDI_TYPE_FSI &&
                            char_type != BIDI_TYPE_PDI)
                        {
                            memcpy(output + out_pos, text + pos, prev_char_len);
                            out_pos += prev_char_len;
                        }
                    }
                }
                else
                { /* LTR */
                    /* Copy characters in original order */
                    int pos = run->start;
                    int end = run->start + run->length;

                    while (pos < end)
                    {
                        int char_len = get_utf8_char_length(text[pos]);

                        /* Skip directional formatting characters */
                        char utf8_buf[4];
                        memcpy(utf8_buf, text + pos, char_len);
                        int codepoint = 0;
                        utf8_to_codepoint(utf8_buf, &codepoint);
                        int char_type = get_char_type(codepoint);

                        /* Skip directional formatting characters in output */
                        if (char_type != BIDI_TYPE_LRE && char_type != BIDI_TYPE_RLE &&
                            char_type != BIDI_TYPE_PDF && char_type != BIDI_TYPE_LRO &&
                            char_type != BIDI_TYPE_RLO && char_type != BIDI_TYPE_LRI &&
                            char_type != BIDI_TYPE_RLI && char_type != BIDI_TYPE_FSI &&
                            char_type != BIDI_TYPE_PDI)
                        {
                            memcpy(output + out_pos, text + pos, char_len);
                            out_pos += char_len;
                        }

                        pos += char_len;
                    }
                }
            }
        }
    }

    return out_pos;
}

/**
 * process_bidirectional_text - Main function for bidirectional text processing
 * @text: Input text
 * @length: Length of input text
 * @is_rtl: Base direction (1 for RTL, 0 for LTR)
 * @output: Output buffer for processed text
 *
 * Return: Length of processed text
 */
int process_bidirectional_text(const char *text, int length, int is_rtl, char *output)
{
    if (!text || !output || length <= 0)
        return 0;

    /* Process text into runs */
    BidiRun *runs = process_runs(text, length, is_rtl ? 1 : 0);
    if (!runs)
        return 0;

    /* Reorder runs */
    int result = reorder_runs(runs, text, length, output);

    /* Clean up */
    while (runs)
    {
        BidiRun *next = runs->next;
        free(runs);
        runs = next;
    }

    return result;
}