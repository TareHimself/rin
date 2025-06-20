# FindWayland.cmake

find_path(XKB_INCLUDE_DIR
    NAMES xkbcommon.h
    PATH_SUFFIXES xkbcommon
)

find_library(XKB_LIBRARY
    NAMES "xkbcommon"
)

include(FindPackageHandleStandardArgs)
find_package_handle_standard_args(XKB
    REQUIRED_VARS
        XKB_INCLUDE_DIR
        XKB_LIBRARY
)

if(XKB_FOUND)
    set(XKB_INCLUDE_DIRS ${XKB_INCLUDE_DIR})
    set(XKB_LIBRARIES ${XKB_LIBRARY})
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
        XKB_INCLUDE_DIR
        XKB_LIBRARY
)
