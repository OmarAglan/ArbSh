cmake_minimum_required(VERSION 3.10)
project(ArbSh C CXX)

# Set C and C++ standards
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 17) # Keep C++17 or higher for std::filesystem
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Option for static linking
option(BUILD_STATIC "Build statically linked executable" OFF)

# Option for GUI mode (now only ImGui)
option(GUI_MODE "Build with GUI support using ImGui" OFF) # Default to ON if that's your main target

# Option for enabling tests
option(ENABLE_TESTS "Build the test suite" OFF)

# --- Define Source File Lists for Libraries ---
set(ARBSH_CORE_SOURCES
    # Core shell logic
    src/core/builtin.c
    src/core/builtin1.c
    src/core/config.c
    src/core/environ.c
    src/core/errors.c
    src/core/errors1.c
    src/core/exits.c
    src/core/getLine.c
    src/core/getenv.c
    src/core/getinfo.c
    src/core/history.c
    src/core/parser.c
    src/core/shell_entry.c
    src/core/shell_loop.c
    src/core/tokenizer.c
    src/core/vars.c
)

set(ARBSH_I18N_SOURCES
    # Internationalization (i18n)
    src/i18n/arabic_input.c
    src/i18n/bidi/bidi.c
    src/i18n/locale/locale.c
)

set(ARBSH_UTILS_SOURCES
    # Utilities
    src/utils/imgui_compat.c
    src/utils/lists.c
    src/utils/lists1.c
    src/utils/memory.c
    src/utils/realloc.c
    src/utils/string.c
    src/utils/string1.c
    src/utils/utf8.c
    src/utils/utf8_output.c
    src/utils/highlight/highlight.c
)
# ------------------------------------------------

# --- Define Libraries ---
add_library(arbsh_utils STATIC ${ARBSH_UTILS_SOURCES})
target_include_directories(arbsh_utils PUBLIC include)

add_library(arbsh_i18n STATIC ${ARBSH_I18N_SOURCES})
target_include_directories(arbsh_i18n PUBLIC include)
target_link_libraries(arbsh_i18n PRIVATE arbsh_utils) # i18n uses utils

# Define Platform Abstraction Library (PAL)
add_library(arbsh_platform STATIC)
target_include_directories(arbsh_platform PUBLIC include)

if(WIN32)
    target_sources(arbsh_platform PRIVATE
        src/platform/windows/process.c
        src/platform/windows/console.c
        src/platform/windows/filesystem.c
        # Add other windows platform source files here
    )
    # Link system libs needed by windows platform implementation
    # We need Shell32.lib for SHGetFolderPathW used in filesystem.c
    target_link_libraries(arbsh_platform PRIVATE shell32)
elseif(UNIX)
    target_sources(arbsh_platform PRIVATE
        src/platform/posix/process.c
        src/platform/posix/console.c
        src/platform/posix/filesystem.c
        # Add other posix platform source files here
    )
    # Link system libs needed by posix platform implementation
    # (e.g., -lpthread for threads, -ldl for dynamic linking if used)
    # target_link_libraries(arbsh_platform PRIVATE pthread dl)
else()
    message(FATAL_ERROR "Unsupported platform for ArbSh PAL")
endif()

add_library(arbsh_core STATIC ${ARBSH_CORE_SOURCES})
target_include_directories(arbsh_core PUBLIC include)
# Core library now depends on the platform abstraction
target_link_libraries(arbsh_core PRIVATE arbsh_i18n arbsh_utils arbsh_platform)

# ------------------------------------------------

# Exclude old entry points if they exist and were caught by glob
list(FILTER ARBSH_CORE_SOURCES EXCLUDE REGEX ".*main\\.c$")
list(FILTER ARBSH_CORE_SOURCES EXCLUDE REGEX ".*main_gui\\.c$")
list(FILTER ARBSH_CORE_SOURCES EXCLUDE REGEX ".*win_main\\.c$")
list(FILTER ARBSH_CORE_SOURCES EXCLUDE REGEX ".*win_gui_common\\.c$")
# ---------------------------------------------------------------------------

# --- Define ImGui Source Files ---
set(IMGUI_DIR "${CMAKE_CURRENT_SOURCE_DIR}/external/imgui")
set(IMGUI_SOURCES
    "${IMGUI_DIR}/imgui.cpp"
    "${IMGUI_DIR}/imgui_demo.cpp"
    "${IMGUI_DIR}/imgui_draw.cpp"
    "${IMGUI_DIR}/imgui_tables.cpp"
    "${IMGUI_DIR}/imgui_widgets.cpp"
    "${IMGUI_DIR}/backends/imgui_impl_win32.cpp"
    "${IMGUI_DIR}/backends/imgui_impl_dx11.cpp"
)
# --------------------------------

# --- Define GUI Specific Source Files ---
# These are files specifically for the GUI mode, including C files they depend on
set(GUI_SPECIFIC_SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/src/gui/imgui_shell.cpp"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/gui/imgui_main.cpp"
    # "${CMAKE_CURRENT_SOURCE_DIR}/src/platform/process_manager.c" # This is platform-specific now
    "${CMAKE_CURRENT_SOURCE_DIR}/src/gui/terminal_tab.c"
)
# ------------------------------------

# --- Platform Specific Sources for Executable ---
# We still need the resource file for Windows builds
set(PLATFORM_EXE_SOURCES "")
if(WIN32)
    if(EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/src/shell.rc")
        list(APPEND PLATFORM_EXE_SOURCES "${CMAKE_CURRENT_SOURCE_DIR}/src/shell.rc")
    endif()
    # If process_manager.c is ONLY used by GUI, add it here conditionally
    if(GUI_MODE)
        list(APPEND PLATFORM_EXE_SOURCES "src/platform/process_manager.c")
    endif()
# elseif(UNIX)
    # No extra sources needed for console executable usually
endif()
# ------------------------------------------------------------

# --- Configure Executable Based on Mode ---
if(WIN32 AND GUI_MODE)
    add_executable(hsh WIN32
        ${GUI_SPECIFIC_SOURCES}
        ${IMGUI_SOURCES}
        ${PLATFORM_EXE_SOURCES} # Add platform sources needed specifically by the EXE
        src/core/shell_entry.c
    )

    # Link the main libraries, platform libs are handled via arbsh_platform dependency in arbsh_core
    target_link_libraries(hsh PRIVATE arbsh_core d3d11 dxgi d3dcompiler dwmapi shell32 user32 gdi32 kernel32 ws2_32)

    target_compile_definitions(hsh PRIVATE USE_IMGUI GUI_MODE)

    target_include_directories(hsh PRIVATE
        include
        ${IMGUI_DIR}
        ${IMGUI_DIR}/backends
        # src/platform # Include comes from arbsh_platform
        src/gui
    )

else()
    # --- Console Application Configuration ---
    add_executable(hsh
         ${PLATFORM_EXE_SOURCES} # Add platform sources needed specifically by the EXE
         src/core/shell_entry.c
    )
    # Link the core library (which transitively links utils, i18n, and platform)
    target_link_libraries(hsh PRIVATE arbsh_core)

    target_include_directories(hsh PRIVATE include) # Core includes come via arbsh_core

    # Platform-specific libraries linked by arbsh_platform or directly if needed by EXE
    if(WIN32)
        target_link_libraries(hsh PRIVATE shell32 user32 gdi32 kernel32 ws2_32)
    # elseif(UNIX)
        # target_link_libraries(hsh PRIVATE pthread dl)
    endif()

endif()
# -----------------------------------------

# --- Common Settings (Apply AFTER target 'hsh' is defined) ---

# Add compile options
if(MSVC)
    target_compile_options(hsh PRIVATE /W3 /wd4273 /wd4996)
    target_compile_definitions(hsh PRIVATE _CRT_SECURE_NO_WARNINGS _CRT_NONSTDC_NO_WARNINGS)
else()
    # Assuming GCC/Clang
    target_compile_options(hsh PRIVATE -Wall -Wextra -Wpedantic)
endif()

# Common Windows definition (applies to both GUI and Console on Windows)
if(WIN32)
    target_compile_definitions(hsh PRIVATE WINDOWS)
endif()

# Static linking if requested
if(BUILD_STATIC)
    if(MSVC)
        # For MSVC, static linking is often controlled by runtime library flags (/MT vs /MD)
        # Setting CMAKE_EXE_LINKER_FLAGS might not be the primary way.
        # Instead, ensure you are targeting the static runtime library.
        # set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>") # Example
        message(WARNING "Static linking with MSVC might require setting CMAKE_MSVC_RUNTIME_LIBRARY.")
        set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STATIC") # May or may not be needed depending on CRT
    else()
        # For GCC/Clang
        # target_link_options(hsh PRIVATE "LINKER:-static") # More modern approach if available
        set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -static-libgcc -static-libstdc++ -static") # Be more specific for GCC
    endif()
    target_compile_definitions(hsh PRIVATE STATIC_BUILD)
endif()

# Set output directories (applies to the 'hsh' target)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# --- Test configuration ---
if(ENABLE_TESTS)
    enable_testing()

    # --- Define common sources needed by multiple tests ---
    set(TEST_COMMON_UTILS_SOURCES
        src/utils/utf8.c
        src/i18n/locale/locale.c
        src/utils/string.c
        src/utils/string1.c
        # Add others like memory.c, realloc.c if needed by tests
    )
    set(TEST_COMMON_INCLUDE_DIRS include tests)
    # -----------------------------------------------------

    # Test for UTF-8 functions
    add_executable(test_utf8
        tests/arabic/text_display/test_utf8.c
        ${TEST_COMMON_UTILS_SOURCES}
    )
    target_include_directories(test_utf8 PRIVATE ${TEST_COMMON_INCLUDE_DIRS})
    if(WIN32)
        target_compile_definitions(test_utf8 PRIVATE WINDOWS _CRT_SECURE_NO_WARNINGS)
    endif()
    add_test(NAME UTF8Test COMMAND test_utf8)

    # Test for bidirectional algorithm
    add_executable(test_bidi
        tests/arabic/bidi_algorithm/test_bidi.c
        src/i18n/bidi/bidi.c          # Specific BiDi source
        ${TEST_COMMON_UTILS_SOURCES}
    )
    target_include_directories(test_bidi PRIVATE ${TEST_COMMON_INCLUDE_DIRS})
    if(WIN32)
        target_compile_definitions(test_bidi PRIVATE WINDOWS _CRT_SECURE_NO_WARNINGS)
    endif()
    add_test(NAME BidiTest COMMAND test_bidi)

    # Test for Arabic keyboard input
    add_executable(test_keyboard
        tests/arabic/text_input/test_keyboard.c
        tests/arabic/text_input/test_stubs.c # Test-specific stubs
        src/i18n/arabic_input.c        # Specific input source
        ${TEST_COMMON_UTILS_SOURCES}
    )
    target_include_directories(test_keyboard PRIVATE ${TEST_COMMON_INCLUDE_DIRS})
    if(WIN32)
        target_compile_definitions(test_keyboard PRIVATE WINDOWS _CRT_SECURE_NO_WARNINGS)
    endif()
    add_test(NAME KeyboardTest COMMAND test_keyboard)

    # If GUI mode is enabled, add GUI-specific tests
    if(GUI_MODE AND WIN32) # GUI tests likely depend on WIN32 and ImGui
        # Test for ImGui integration (Example - adjust sources as needed)
        add_executable(test_imgui WIN32 # Needs WIN32 for ImGui backend
            tests/gui/test_imgui.cpp
            # Link necessary ImGui sources or a minimal ImGui test setup
            ${IMGUI_SOURCES}
            # Link common utils if needed by test_imgui.cpp
            ${TEST_COMMON_UTILS_SOURCES}
        )
        target_include_directories(test_imgui PRIVATE ${TEST_COMMON_INCLUDE_DIRS} ${IMGUI_DIR} ${IMGUI_DIR}/backends)
        target_compile_definitions(test_imgui PRIVATE WINDOWS _CRT_SECURE_NO_WARNINGS USE_IMGUI GUI_MODE) # Define GUI mode for test too
        # Link libraries needed by ImGui test
        target_link_libraries(test_imgui PRIVATE arbsh_utils arbsh_i18n d3d11 dxgi d3dcompiler dwmapi user32 gdi32)
        add_test(NAME ImGuiTest COMMAND test_imgui)
    endif()

    # Define a custom test target to run all tests
    # List explicit test targets as dependencies
    set(TEST_TARGETS test_utf8 test_bidi test_keyboard)
    if(GUI_MODE AND WIN32)
        list(APPEND TEST_TARGETS test_imgui)
    endif()

    add_custom_target(run_tests
        COMMAND ${CMAKE_CTEST_COMMAND} --output-on-failure --verbose # Add verbose for more output
        DEPENDS ${TEST_TARGETS}
        COMMENT "Running all tests"
    )

endif() # ENABLE_TESTS

# --- Install rules ---
install(TARGETS hsh
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)

# --- Documentation ---
find_package(Doxygen)
if(DOXYGEN_FOUND)
    set(DOXYGEN_IN ${CMAKE_CURRENT_SOURCE_DIR}/docs/Doxyfile.in)
    set(DOXYGEN_OUT ${CMAKE_CURRENT_BINARY_DIR}/Doxyfile)

    # Pass project version info to Doxygen if available
    # set(DOXYGEN_PROJECT_NUMBER ${PROJECT_VERSION}) # Example if you set PROJECT_VERSION

    configure_file(${DOXYGEN_IN} ${DOXYGEN_OUT} @ONLY)

    add_custom_target(docs ALL # Add ALL to build docs by default with 'make' or 'cmake --build .'
        COMMAND ${DOXYGEN_EXECUTABLE} ${DOXYGEN_OUT}
        WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
        COMMENT "Generating API documentation with Doxygen"
        VERBATIM
    )
else()
     message(STATUS "Doxygen not found. Documentation target 'docs' will not be available.")
endif()