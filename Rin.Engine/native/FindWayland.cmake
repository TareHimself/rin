# FindWayland.cmake

find_path(WAYLAND_INCLUDE_DIR
    NAMES wayland-client.h
    PATH_SUFFIXES wayland
)

find_library(WAYLAND_LIBRARY
    NAMES wayland-client
)

find_program(WAYLAND_SCANNER_EXECUTABLE
    NAMES wayland-scanner
)

find_path(WAYLAND_PROTOCOLS_DIR
        NAMES stable/xdg-shell/xdg-shell.xml
        PATHS
        /usr/share/wayland-protocols
        /usr/local/share/wayland-protocols
        ${CMAKE_INSTALL_PREFIX}/share/wayland-protocols
        DOC "Base path to wayland-protocols"
)

if(NOT WAYLAND_PROTOCOLS_DIR)
    message(FATAL_ERROR "Unable to find wayland protocols. >> sudo apt install wayland-protocols")
endif()

# Protocol definitions
set(PROTOCOLS
    stable/xdg-shell/xdg-shell.xml
    unstable/xdg-decoration/xdg-decoration-unstable-v1.xml
#    unstable/relative-pointer/relative-pointer-unstable-v1.xml
#    unstable/pointer-constraints/pointer-constraints-unstable-v1.xml
#    stable/viewporter/viewporter.xml
)

set(GENERATED_HEADERS "")
set(GENERATED_SOURCES "")
set(GENERATED_OUTPUT_DIR "${CMAKE_CURRENT_BINARY_DIR}/generated/wayland")
list(APPEND WAYLAND_INCLUDE_DIR ${GENERATED_OUTPUT_DIR})
file(MAKE_DIRECTORY ${GENERATED_OUTPUT_DIR})
foreach(xml IN LISTS PROTOCOLS)
    get_filename_component(name "${xml}" NAME_WE)
    string(REPLACE "-" "_" cname ${name})  # safer variable names
    set(header "${GENERATED_OUTPUT_DIR}/${name}-client-protocol.h")
    set(source "${GENERATED_OUTPUT_DIR}/${name}-protocol.c")

    list(APPEND GENERATED_HEADERS ${header})
    list(APPEND GENERATED_SOURCES ${source})

    if(NOT EXISTS "${header}")
        message(STATUS "Generating ${name}")
        execute_process(COMMAND ${WAYLAND_SCANNER_EXECUTABLE} client-header ${WAYLAND_PROTOCOLS_DIR}/${xml} ${header})
        if(NOT EXISTS "${header}")
            message(FATAL_ERROR "Failed to generate ${name}")
        endif()
    endif()

    if(NOT EXISTS "${source}")
        message(STATUS "Generating ${name}")
        execute_process(COMMAND ${WAYLAND_SCANNER_EXECUTABLE} private-code  ${WAYLAND_PROTOCOLS_DIR}/${xml} ${source})
        if(NOT EXISTS "${source}")
            message(FATAL_ERROR "Failed to generate ${name}")
        endif()
    endif()
#    add_custom_command(
#        OUTPUT ${header} ${source}
#        COMMAND ${WAYLAND_SCANNER_EXECUTABLE} client-header ${WAYLAND_PROTOCOLS_DIR}/${xml} ${header}
#        COMMAND ${WAYLAND_SCANNER_EXECUTABLE} private-code  ${WAYLAND_PROTOCOLS_DIR}/${xml} ${source}
#        DEPENDS ${WAYLAND_PROTOCOLS_DIR}/${xml}
#        COMMENT "Generating Wayland protocol: ${xml}"
#        VERBATIM
#    )
endforeach()

#add_custom_target(GenerateWaylandProtocols
#    DEPENDS ${GENERATED_HEADERS} ${GENERATED_SOURCES}
#)

include(FindPackageHandleStandardArgs)
find_package_handle_standard_args(Wayland
    REQUIRED_VARS
        WAYLAND_INCLUDE_DIR
        WAYLAND_LIBRARY
        WAYLAND_SCANNER_EXECUTABLE
        WAYLAND_PROTOCOLS_DIR
)

if(WAYLAND_FOUND)
    set(WAYLAND_INCLUDE_DIRS ${WAYLAND_INCLUDE_DIR})
    set(WAYLAND_LIBRARIES ${WAYLAND_LIBRARY})
    set(WAYLAND_SCANNER ${WAYLAND_SCANNER_EXECUTABLE})

    set(WAYLAND_GENERATED_HEADERS ${GENERATED_HEADERS})
    set(WAYLAND_GENERATED_SOURCES ${GENERATED_SOURCES})
endif()

## Optional: wayland-cursor (used for pointer images)
#find_path(WAYLAND_CURSOR_INCLUDE_DIR
#    NAMES wayland-cursor.h
#    PATH_SUFFIXES wayland
#)
#
#find_library(WAYLAND_CURSOR_LIBRARY
#    NAMES wayland-cursor
#)

#if(WAYLAND_CURSOR_LIBRARY AND WAYLAND_CURSOR_INCLUDE_DIR)
#    set(WAYLAND_CURSOR_LIBRARIES ${WAYLAND_CURSOR_LIBRARY})
#    set(WAYLAND_CURSOR_INCLUDE_DIRS ${WAYLAND_CURSOR_INCLUDE_DIR})
#endif()

mark_as_advanced(
    WAYLAND_INCLUDE_DIR
    WAYLAND_LIBRARY
    WAYLAND_SCANNER_EXECUTABLE
    WAYLAND_PROTOCOLS_DIR
#    WAYLAND_CURSOR_INCLUDE_DIR
#    WAYLAND_CURSOR_LIBRARY
)
