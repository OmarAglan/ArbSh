cmake_minimum_required(VERSION 3.10)
project(ArbSh C CXX)

# Set C and C++ standards
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Option for static linking
option(BUILD_STATIC "Build statically linked executable" OFF)

# Option for GUI mode (now only ImGui)
option(GUI_MODE "Build with GUI support using ImGui" ON)

# Option for enabling tests
option(ENABLE_TESTS "Build the test suite" OFF)

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

# ImGui sources
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

# Create a new variable for GUI sources
set(GUI_SOURCES
    "src/gui/imgui_shell.cpp"
    "src/gui/imgui_main.cpp"
)

# Explicitly add the UTF-8 and localization source files
list(APPEND SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/src/utils/utf8.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/utils/utf8_output.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/i18n/locale/locale.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/core/shell_entry.c"
)

# Add compatibility file for ImGui
if(GUI_MODE)
    list(APPEND SOURCES
        "${CMAKE_CURRENT_SOURCE_DIR}/src/utils/imgui_compat.c"
    )
endif()

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
if(WIN32 AND GUI_MODE)
    # Windows GUI application with ImGui
    add_executable(hsh WIN32 ${SOURCES} ${IMGUI_SOURCES} ${GUI_SOURCES})
    
    # Add GUI-related definitions
    target_compile_definitions(hsh PRIVATE USE_IMGUI GUI_MODE)
else()
    # Console application
    add_executable(hsh ${SOURCES})
endif()

    # Add process manager and terminal tab sources
    list(APPEND SOURCES 
        "${CMAKE_CURRENT_SOURCE_DIR}/src/platform/process_manager.c"
        "${CMAKE_CURRENT_SOURCE_DIR}/src/gui/terminal_tab.c"
    )

# Include directories
target_include_directories(hsh PRIVATE 
    include 
    ${IMGUI_DIR}
    ${IMGUI_DIR}/backends
    # Add include directory for platform if process_manager.h is there
    "${CMAKE_CURRENT_SOURCE_DIR}/src/platform" 
    "${CMAKE_CURRENT_SOURCE_DIR}/src/gui" # For terminal_tab.h
)

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
    
    # Add GUI-specific libraries
    if(GUI_MODE)
        # Add Direct3D libraries for ImGui
        target_link_libraries(hsh PRIVATE d3d11 dxgi d3dcompiler)
        
        # Add dwmapi for ImGui
        target_link_libraries(hsh PRIVATE dwmapi)
    endif()
endif()

# Set output directories
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# Test configuration (only when ENABLE_TESTS is ON)
if(ENABLE_TESTS)
    # Enable testing
    enable_testing()
    
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
        src/i18n/locale/locale.c
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
    
    # If GUI mode is enabled, add GUI-specific tests
    if(GUI_MODE)
        # Test for ImGui integration
        add_executable(test_imgui
            tests/gui/test_imgui.cpp
            src/utils/utf8.c
        )
        target_include_directories(test_imgui PRIVATE include tests ${IMGUI_DIR})
        if(WIN32)
            target_compile_definitions(test_imgui PRIVATE WINDOWS _CRT_SECURE_NO_WARNINGS)
            target_link_libraries(test_imgui PRIVATE user32 gdi32)
        endif()
        add_test(NAME ImGuiTest COMMAND test_imgui)
    endif()
    
    # Define a custom test target to run all tests
    add_custom_target(run_tests
        COMMAND ${CMAKE_CTEST_COMMAND} --output-on-failure
        DEPENDS test_utf8 test_bidi test_keyboard
        COMMENT "Running all tests"
    )
    
    # If GUI mode is enabled, add GUI tests to run_tests dependencies
    if(GUI_MODE)
        add_dependencies(run_tests test_imgui)
    endif()
endif()

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
