cmake_minimum_required(VERSION 3.10)
project(simple_shell C)

# Set C standard
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

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
    "src/*.c"
)

# Define header files
file(GLOB_RECURSE HEADERS 
    "include/*.h"
)

# Add executable
add_executable(hsh ${SOURCES})

# Include directories
target_include_directories(hsh PRIVATE include)

# Add platform-specific definitions and libraries
if(WIN32)
    target_compile_definitions(hsh PRIVATE 
        WINDOWS 
        _CRT_SECURE_NO_WARNINGS
        _CRT_NONSTDC_NO_WARNINGS
    )
endif()

# Set output directories
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# Enable testing
enable_testing()

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
