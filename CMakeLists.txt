cmake_minimum_required(VERSION 3.10)
project(ArbSh C)

# Set C standard
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Option for static linking
option(BUILD_STATIC "Build statically linked executable" OFF)

# Option for Windows GUI mode
option(WIN_GUI "Build as a Windows GUI application" OFF)

# Add compile options
if(MSVC)
    # MSVC specific settings
    add_definitions(-D_CRT_SECURE_NO_WARNINGS)
    add_compile_options(/W3)  # Warning level 3
    
    # Disable specific warnings
    add_compile_options(
        /wd4273  # Disable warning about inconsistent dll linkage
        /wd4996  # Disable deprecated function warnings
    )
else()
    add_compile_options(-Wall -Wextra -pedantic)
endif()

# Define source files
file(GLOB_RECURSE SOURCES
    "src/core/*.c"
    "src/i18n/*.c"
    "src/utils/*.c"
    "src/platform/windows/*.c"
)

# Special handling for entry point files
# Use the new unified shell_entry.c instead of the old entry point files
list(FILTER SOURCES EXCLUDE REGEX ".*main\\.c$")      # Exclude main.c
list(FILTER SOURCES EXCLUDE REGEX ".*main_gui\\.c$")  # Exclude main_gui.c
list(FILTER SOURCES EXCLUDE REGEX ".*win_main\\.c$")  # Exclude win_main.c
list(FILTER SOURCES EXCLUDE REGEX ".*win_gui_common\\.c$")  # Exclude win_gui_common.c

# Explicitly add the UTF-8 and localization source files
list(APPEND SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/src/utils/utf8.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/utils/utf8_output.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/i18n/locale/locale.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/core/shell_entry.c"
)

# Define header files
file(GLOB_RECURSE HEADERS 
    "include/*.h"
)

# Add Windows resource file for icon if it exists
if(WIN32)
    if(EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/src/shell.rc")
        list(APPEND SOURCES "${CMAKE_CURRENT_SOURCE_DIR}/src/shell.rc")
    endif()
endif()

# Add executable
if(WIN32 AND WIN_GUI)
    # Windows GUI application
    add_executable(hsh WIN32 ${SOURCES})
else()
    # Console application
    add_executable(hsh ${SOURCES})
endif()

# Include directories
target_include_directories(hsh PRIVATE include)

# Static linking if requested
if(BUILD_STATIC)
    if(MSVC)
        # Static linking for Windows
        set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STATIC")
    else()
        # Static linking for Unix/Linux
        set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -static")
    endif()
    
    # Define static build
    target_compile_definitions(hsh PRIVATE STATIC_BUILD)
endif()

# Add platform-specific definitions and libraries
if(WIN32)
    target_compile_definitions(hsh PRIVATE 
        WINDOWS 
        _CRT_SECURE_NO_WARNINGS
        _CRT_NONSTDC_NO_WARNINGS
    )
    
    # Add Windows-specific libraries
    target_link_libraries(hsh PRIVATE shell32 user32 gdi32 kernel32)
    
    # Add comctl32 for Windows Common Controls - always needed for shell_entry.c
    target_link_libraries(hsh PRIVATE comctl32)
    
    # Add additional definitions for GUI mode
    if(WIN_GUI)
        target_compile_definitions(hsh PRIVATE _WIN32_IE=0x0600 WIN_GUI)
    endif()
endif()

# Set output directories
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# Enable testing
enable_testing()

# Add test targets
# Test for UTF-8 functions
add_executable(test_utf8 
    tests/arabic/text_display/test_utf8.c
    src/utils/utf8.c
    src/i18n/locale/locale.c
)
target_include_directories(test_utf8 PRIVATE include tests)
if(WIN32)
    target_compile_definitions(test_utf8 PRIVATE WINDOWS _CRT_SECURE_NO_WARNINGS)
endif()
add_test(NAME UTF8Test COMMAND test_utf8)

# Test for bidirectional algorithm
add_executable(test_bidi 
    tests/arabic/bidi_algorithm/test_bidi.c
    src/i18n/bidi/bidi.c
    src/utils/utf8.c
)
target_include_directories(test_bidi PRIVATE include tests)
if(WIN32)
    target_compile_definitions(test_bidi PRIVATE WINDOWS _CRT_SECURE_NO_WARNINGS)
endif()
add_test(NAME BidiTest COMMAND test_bidi)

# Test for Arabic keyboard input
add_executable(test_keyboard 
    tests/arabic/text_input/test_keyboard.c
    tests/arabic/text_input/test_stubs.c
    src/i18n/arabic_input.c
    src/i18n/locale/locale.c
    src/utils/utf8.c
    src/utils/string.c
    src/utils/string1.c
)
target_include_directories(test_keyboard PRIVATE include tests)
if(WIN32)
    target_compile_definitions(test_keyboard PRIVATE WINDOWS _CRT_SECURE_NO_WARNINGS)
endif()
add_test(NAME KeyboardTest COMMAND test_keyboard)

# Define a custom test target to run all tests
add_custom_target(run_tests
    COMMAND ${CMAKE_CTEST_COMMAND} --output-on-failure
    DEPENDS test_utf8 test_bidi test_keyboard
    COMMENT "Running all tests"
)

# Install rules
install(TARGETS hsh
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)

# Documentation
find_package(Doxygen)
if(DOXYGEN_FOUND)
    set(DOXYGEN_IN ${CMAKE_CURRENT_SOURCE_DIR}/docs/Doxyfile.in)
    set(DOXYGEN_OUT ${CMAKE_CURRENT_BINARY_DIR}/Doxyfile)

    configure_file(${DOXYGEN_IN} ${DOXYGEN_OUT} @ONLY)

    add_custom_target(docs
        COMMAND ${DOXYGEN_EXECUTABLE} ${DOXYGEN_OUT}
        WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
        COMMENT "Generating API documentation with Doxygen"
        VERBATIM
    )
endif()
