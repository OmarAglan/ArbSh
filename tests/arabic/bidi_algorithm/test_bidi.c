#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "../../test_helpers.h"
#include "../../fixtures/text_samples/arabic_samples.h"

/**
 * test_char_type_detection - Test bidirectional character type detection
 */
int test_char_type_detection(void) {
    TEST_SECTION("Bidirectional Character Type Detection");
    
    /* Test Arabic letter (AL) */
    int arabic_codepoint = 0x0627; /* 'ا' (U+0627) */
    int char_type = get_char_type(arabic_codepoint);
    ASSERT_INT_EQUAL(char_type, 12, "Arabic letter should be type AL (12)"); // AL = 12 from the bidi.c defines
    
    /* Test Latin letter (L) */
    int latin_codepoint = 'A'; /* 'A' (U+0041) */
    char_type = get_char_type(latin_codepoint);
    ASSERT_INT_EQUAL(char_type, 0, "Latin letter should be type L (0)"); // L = 0 from the bidi.c defines
    
    /* Test number (EN) */
    int number_codepoint = '5'; /* '5' (U+0035) */
    char_type = get_char_type(number_codepoint);
    ASSERT_INT_EQUAL(char_type, 2, "Number should be type EN (2)"); // EN = 2 from the bidi.c defines
    
    /* Test RTL mark */
    int rtl_mark = 0x200F; /* U+200F RLM */
    char_type = get_char_type(rtl_mark);
    ASSERT_INT_EQUAL(char_type, 23, "RLM should be type RLM (23)"); // RLM = 23 from the bidi.c defines
    
    TEST_REPORT(TEST_PASS, "Bidirectional character type detection tests passed");
    return TEST_PASS;
}

/**
 * test_bidi_text_processing - Test bidirectional text processing
 */
int test_bidi_text_processing(void) {
    TEST_SECTION("Bidirectional Text Processing");
    
    /* Test simple Arabic text */
    const char *input = ARABIC_GREETING;
    char output[256] = {0};
    int result = process_bidirectional_text(input, strlen(input), 1, output);
    
    ASSERT_TRUE(result > 0, "Bidirectional processing should succeed");
    
    /* Test complex mixed text */
    input = MIXED_TEXT;
    memset(output, 0, sizeof(output));
    result = process_bidirectional_text(input, strlen(input), 1, output);
    
    ASSERT_TRUE(result > 0, "Complex bidirectional processing should succeed");
    
    /* Test nested bidirectional text */
    input = NESTED_BIDI;
    memset(output, 0, sizeof(output));
    result = process_bidirectional_text(input, strlen(input), 1, output);
    
    ASSERT_TRUE(result > 0, "Nested bidirectional processing should succeed");
    
    TEST_REPORT(TEST_PASS, "Bidirectional text processing tests passed");
    return TEST_PASS;
}

/**
 * test_direction_control - Test directional control characters
 */
int test_direction_control(void) {
    TEST_SECTION("Directional Control Characters");
    
    /* Create a test string with explicit directional controls */
    char test_string[32] = {0};
    
    /* RLM + Arabic + LRM */
    test_string[0] = 0xE2; test_string[1] = 0x80; test_string[2] = 0x8F; /* RLM */
    test_string[3] = 0xD8; test_string[4] = 0xA7; /* 'ا' */
    test_string[5] = 0xE2; test_string[6] = 0x80; test_string[7] = 0x8E; /* LRM */
    test_string[8] = 0;
    
    char output[32] = {0};
    int result = process_bidirectional_text(test_string, strlen(test_string), 1, output);
    
    ASSERT_TRUE(result > 0, "Directional control processing should succeed");
    
    TEST_REPORT(TEST_PASS, "Directional control character tests passed");
    return TEST_PASS;
}

int main(void) {
    TestCase tests[] = {
        {"Character Type Detection", test_char_type_detection},
        {"Bidirectional Text Processing", test_bidi_text_processing},
        {"Directional Control Characters", test_direction_control}
    };
    
    return run_test_suite("Bidirectional Algorithm", tests, sizeof(tests) / sizeof(tests[0]));
} 