# FindWayland.cmake

find_path(DECOR_INCLUDE_DIR
    NAMES libdecor.h
    PATH_SUFFIXES libdecor-0
)

find_library(DECOR_LIBRARY
    NAMES "decor-0"
)

include(FindPackageHandleStandardArgs)
find_package_handle_standard_args(Decor
    REQUIRED_VARS
        DECOR_INCLUDE_DIR
        DECOR_LIBRARY
)

if(DECOR_FOUND)
    set(DECOR_INCLUDE_DIRS ${DECOR_INCLUDE_DIR})
    set(DECOR_LIBRARIES ${DECOR_LIBRARY})
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
