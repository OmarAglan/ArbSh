#ifndef _BIDI_H_
#define _BIDI_H_

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

/* Function declarations */
void init_bidi(void);
int process_bidirectional_text(const char *text, int length, int is_rtl, char *output);
int get_char_type(int codepoint);

#endif /* _BIDI_H_ */ 