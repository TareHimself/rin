cmake_minimum_required(VERSION 3.10 FATAL_ERROR)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)

include(${CMAKE_CURRENT_SOURCE_DIR}/utils.cmake)

# include(ExternalProject)
if (CMAKE_BUILD_TYPE STREQUAL "Debug")
    set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
endif()

include(FetchContent)

FetchContent_Declare(fetch_vkma
       GIT_REPOSITORY https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator
       GIT_TAG v3.1.0
       GIT_SHALLOW 1
)
FetchContent_Declare(fetch_vkb
       GIT_REPOSITORY https://github.com/charles-lunarg/vk-bootstrap
       GIT_TAG v1.3.283
       GIT_SHALLOW 1
)
FetchContent_Declare(fetch_msdfgen
       GIT_REPOSITORY https://github.com/Chlumsky/msdfgen
       GIT_TAG v1.12
       GIT_SHALLOW 1
)


set(BUILD_SHARED_LIBS ON)
set(VK_BOOTSTRAP_TEST OFF)
set(MSDFGEN_DYNAMIC_RUNTIME ON)
set(BUILD_SHARED_LIBS ON)
set(MSDFGEN_USE_SKIA OFF)
set(MSDFGEN_INSTALL ON)
set(MSDFGEN_CORE_ONLY ON)
set(MSDFGEN_BUILD_STANDALONE OFF)
set(MSDFGEN_USE_VCPKG OFF)


FetchContent_MakeAvailable(fetch_vkma fetch_vkb fetch_msdfgen)


project(Rin.Engine.Native LANGUAGES C CXX VERSION 1.0.0 DESCRIPTION "Native abstractions for the Rin.Engine")
set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)
file(GLOB_RECURSE SOURCE_FILES "${SRC_DIR}/*.cpp" "${SRC_DIR}/*.hpp")

option(RIN_BUILD_TEST_MAIN "Build a test main file for debugging" OFF)
set(RIN_EXTERNAL_DIR ${CMAKE_CURRENT_BINARY_DIR}/external)

if(LINUX)
    find_package(Wayland REQUIRED)
    list(APPEND SOURCE_FILES ${WAYLAND_GENERATED_SOURCES})
endif()

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})

if(LINUX)
    find_package(Decor REQUIRED)
    find_package(XKB REQUIRED)
    target_include_directories(${PROJECT_NAME} PUBLIC  ${DECOR_INCLUDE_DIRS} ${XKB_INCLUDE_DIRS} ${WAYLAND_INCLUDE_DIRS})
    target_link_libraries(${PROJECT_NAME} ${DECOR_LIBRARIES} ${XKB_LIBRARIES} ${WAYLAND_LIBRARIES})
endif()

target_include_directories(${PROJECT_NAME} PUBLIC ${SRC_DIR} ${RIN_EXTERNAL_DIR})

find_package(Vulkan REQUIRED)
find_package(webmdx REQUIRED)
target_link_libraries(${PROJECT_NAME} webmdx::webmdx msdfgen::msdfgen Vulkan::Vulkan vk-bootstrap::vk-bootstrap GPUOpen::VulkanMemoryAllocator ${WAYLAND_LIBRARIES})
FindSlang(${PROJECT_NAME} 2025.9.2)
GetBass()


add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_RUNTIME_DLLS:${PROJECT_NAME}> $<TARGET_FILE_DIR:${PROJECT_NAME}>
        COMMAND_EXPAND_LISTS
)
#add_custom_command ( TARGET ${PROJECT_NAME} POST_BUILD
#    COMMAND conda activate && python ${CMAKE_CURRENT_SOURCE_DIR}/../../copy_s.py "$<TARGET_RUNTIME_DLLS:${PROJECT_NAME}>" ${TARGET_DIR}
#)


if(RIN_BUILD_TEST_MAIN)
    set(STB_IMAGE_WRITE_DIR ${CMAKE_CURRENT_BINARY_DIR}/external/stb)
    set(STB_IMAGE_WRITE_FILE ${STB_IMAGE_WRITE_DIR}/stb_image_write.h)
    if(NOT EXISTS ${STB_IMAGE_WRITE_FILE})
        file(MAKE_DIRECTORY ${STB_IMAGE_WRITE_DIR})
        file(DOWNLOAD  "https://raw.githubusercontent.com/nothings/stb/refs/heads/master/stb_image_write.h" ${STB_IMAGE_WRITE_FILE} SHOW_PROGRESS)
    endif()
    add_executable(test ${CMAKE_CURRENT_LIST_DIR}/test/main.cpp)
    target_link_libraries(test ${PROJECT_NAME})
    target_include_directories(test PRIVATE ${CMAKE_CURRENT_LIST_DIR}/src ${STB_IMAGE_WRITE_DIR})
    add_custom_command(TARGET test POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_RUNTIME_DLLS:test> $<TARGET_FILE_DIR:test>
            COMMAND_EXPAND_LISTS
    )
#    add_custom_command ( TARGET test POST_BUILD
#            COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/../../copy_s.py "$<TARGET_RUNTIME_DLLS:test>" ${TARGET_DIR}
#    )
endif()


