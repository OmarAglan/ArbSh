#ifndef TEST_HELPERS_H
#define TEST_HELPERS_H

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "../include/shell.h"

/* Test result codes */
#define TEST_PASS 0
#define TEST_FAIL 1

/* Colors for test output */
#define COLOR_RED    "\x1b[31m"
#define COLOR_GREEN  "\x1b[32m"
#define COLOR_YELLOW "\x1b[33m"
#define COLOR_BLUE   "\x1b[34m"
#define COLOR_RESET  "\x1b[0m"

/* Test reporting macros */
#define TEST_REPORT(result, message) \
    printf("%s[%s]%s %s\n", \
        (result) == TEST_PASS ? COLOR_GREEN : COLOR_RED, \
        (result) == TEST_PASS ? "PASS" : "FAIL", \
        COLOR_RESET, \
        (message))

#define TEST_SECTION(name) \
    printf("\n%s--- %s ---%s\n", COLOR_BLUE, (name), COLOR_RESET)

#define TEST_WARNING(message) \
    printf("%s[WARN]%s %s\n", COLOR_YELLOW, COLOR_RESET, (message))

#define ASSERT_TRUE(condition, message) \
    do { \
        if (!(condition)) { \
            TEST_REPORT(TEST_FAIL, message); \
            return TEST_FAIL; \
        } \
    } while (0)

#define ASSERT_STRING_EQUAL(str1, str2, message) \
    do { \
        if (strcmp((str1), (str2)) != 0) { \
            printf("%s[FAIL]%s %s: \"%s\" != \"%s\"\n", \
                COLOR_RED, COLOR_RESET, (message), (str1), (str2)); \
            return TEST_FAIL; \
        } \
    } while (0)

#define ASSERT_INT_EQUAL(val1, val2, message) \
    do { \
        if ((val1) != (val2)) { \
            printf("%s[FAIL]%s %s: %d != %d\n", \
                COLOR_RED, COLOR_RESET, (message), (val1), (val2)); \
            return TEST_FAIL; \
        } \
    } while (0)

/* Test runner functions */
typedef int (*TestFunction)(void);

typedef struct {
    const char *name;
    TestFunction func;
} TestCase;

/* Run a suite of tests and report results */
int run_test_suite(const char *suite_name, TestCase tests[], int test_count) {
    int pass_count = 0;
    int fail_count = 0;
    
    printf("\n%s=== Test Suite: %s ===%s\n", COLOR_BLUE, suite_name, COLOR_RESET);
    
    for (int i = 0; i < test_count; i++) {
        printf("\n%s--- Test: %s ---%s\n", COLOR_YELLOW, tests[i].name, COLOR_RESET);
        int result = tests[i].func();
        
        if (result == TEST_PASS) {
            pass_count++;
        } else {
            fail_count++;
        }
    }
    
    printf("\n%s=== Test Summary: %s ===%s\n", COLOR_BLUE, suite_name, COLOR_RESET);
    printf("Tests: %d, Passed: %s%d%s, Failed: %s%d%s\n", 
        test_count, 
        COLOR_GREEN, pass_count, COLOR_RESET,
        fail_count > 0 ? COLOR_RED : COLOR_RESET, fail_count, COLOR_RESET);
    
    return fail_count;
}

#endif /* TEST_HELPERS_H */ 