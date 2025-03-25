#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "../../test_helpers.h"
#include "../../fixtures/text_samples/arabic_samples.h"

/**
 * test_utf8_char_length - Test the UTF-8 character length detection
 */
int test_utf8_char_length(void) {
    TEST_SECTION("UTF-8 Character Length Detection");
    
    /* Test ASCII character (single byte) */
    char ascii = 'A';
    int length = get_utf8_char_length(ascii);
    ASSERT_INT_EQUAL(length, 1, "ASCII character should be 1 byte");
    
    /* Test 2-byte UTF-8 character */
    char utf8_2byte = 0xC3;  /* First byte of 'é' (U+00E9) in UTF-8: 0xC3 0xA9 */
    length = get_utf8_char_length(utf8_2byte);
    ASSERT_INT_EQUAL(length, 2, "2-byte UTF-8 character detection");
    
    /* Test 3-byte UTF-8 character (Arabic letter) */
    char utf8_3byte = 0xD9;  /* First byte of 'م' (U+0645) in UTF-8: 0xD9 0x85 */
    length = get_utf8_char_length(utf8_3byte);
    ASSERT_INT_EQUAL(length, 2, "3-byte UTF-8 character detection"); // Arabic characters are 2-byte in UTF-8
    
    /* Test invalid UTF-8 first byte */
    char invalid = 0xFF;
    length = get_utf8_char_length(invalid);
    ASSERT_INT_EQUAL(length, 1, "Invalid UTF-8 should return 1");
    
    TEST_REPORT(TEST_PASS, "UTF-8 character length detection tests passed");
    return TEST_PASS;
}

/**
 * test_utf8_codepoint_conversion - Test UTF-8 to codepoint conversion
 */
int test_utf8_codepoint_conversion(void) {
    TEST_SECTION("UTF-8 to Codepoint Conversion");
    
    /* Test ASCII character conversion */
    char ascii[2] = "A";
    int codepoint = 0;
    int result = utf8_to_codepoint(ascii, &codepoint);
    ASSERT_TRUE(result, "ASCII to codepoint conversion should succeed");
    ASSERT_INT_EQUAL(codepoint, 65, "ASCII 'A' should convert to codepoint 65");
    
    /* Test Arabic character conversion */
    char arabic[3] = {0xD9, 0x85, 0}; /* 'م' (U+0645) in UTF-8 */
    result = utf8_to_codepoint(arabic, &codepoint);
    ASSERT_TRUE(result, "Arabic to codepoint conversion should succeed");
    ASSERT_INT_EQUAL(codepoint, 0x0645, "Arabic 'م' should convert to codepoint 0x0645");
    
    /* Test codepoint to UTF-8 conversion */
    char utf8_buf[4] = {0};
    int utf8_len = codepoint_to_utf8(0x0645, utf8_buf);
    ASSERT_INT_EQUAL(utf8_len, 2, "Codepoint 0x0645 should convert to 2 bytes in UTF-8");
    
    /* Print the actual bytes for debugging */
    printf("Converted 0x0645 to bytes: 0x%02X 0x%02X\n", 
           (unsigned char)utf8_buf[0], (unsigned char)utf8_buf[1]);
           
    /* Check the bytes in a way that works with signed char */
    ASSERT_TRUE(((unsigned char)utf8_buf[0] == 0xD9) && 
                ((unsigned char)utf8_buf[1] == 0x85),
                "Codepoint 0x0645 should convert to 0xD9 0x85 in UTF-8");
    
    TEST_REPORT(TEST_PASS, "UTF-8 codepoint conversion tests passed");
    return TEST_PASS;
}

/**
 * test_is_rtl_char - Test RTL character detection
 */
int test_is_rtl_char(void) {
    TEST_SECTION("RTL Character Detection");
    
    /* Test Arabic character */
    int arabic_codepoint = 0x0645; /* 'م' (U+0645) */
    int is_rtl = is_rtl_char(arabic_codepoint);
    ASSERT_TRUE(is_rtl, "Arabic character should be detected as RTL");
    
    /* Test Hebrew character */
    int hebrew_codepoint = 0x05D0; /* 'א' (U+05D0) */
    is_rtl = is_rtl_char(hebrew_codepoint);
    ASSERT_TRUE(is_rtl, "Hebrew character should be detected as RTL");
    
    /* Test Latin character */
    int latin_codepoint = 0x0041; /* 'A' (U+0041) */
    is_rtl = is_rtl_char(latin_codepoint);
    ASSERT_TRUE(!is_rtl, "Latin character should not be detected as RTL");
    
    TEST_REPORT(TEST_PASS, "RTL character detection tests passed");
    return TEST_PASS;
}

int main(void) {
    TestCase tests[] = {
        {"UTF-8 Character Length", test_utf8_char_length},
        {"UTF-8 Codepoint Conversion", test_utf8_codepoint_conversion},
        {"RTL Character Detection", test_is_rtl_char}
    };
    
    return run_test_suite("UTF-8 Functions", tests, sizeof(tests) / sizeof(tests[0]));
}