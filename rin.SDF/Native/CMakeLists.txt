cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
include(FetchContent)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
include(${CMAKE_CURRENT_SOURCE_DIR}/utils.cmake)

set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
# include(ExternalProject)

project(rin.SdfN LANGUAGES CXX VERSION 1.0.0 DESCRIPTION "Native abstractions for the rin.SdfN")

set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)

file(GLOB_RECURSE SOURCE_FILES "${SRC_DIR}/*.cpp" "${SRC_DIR}/*.hpp")

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})

target_include_directories(${PROJECT_NAME} PUBLIC ${SRC_DIR})

FetchContent_Declare(fetch_msdfgen
    GIT_REPOSITORY https://github.com/Chlumsky/msdfgen
    GIT_TAG v1.12
    GIT_SHALLOW 1
)
set(MSDFGEN_DYNAMIC_RUNTIME ON)
set(BUILD_SHARED_LIBS ON)
set(MSDFGEN_USE_SKIA OFF)
set(MSDFGEN_INSTALL ON)
set(MSDFGEN_CORE_ONLY ON)
set(MSDFGEN_BUILD_STANDALONE OFF) 
set(MSDFGEN_USE_VCPKG OFF)
FetchContent_MakeAvailable(fetch_msdfgen)
target_link_libraries(${PROJECT_NAME} msdfgen::msdfgen)
SetDynamicLibraryDir(${PROJECT_NAME})

add_custom_command ( TARGET ${PROJECT_NAME} POST_BUILD
    COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/../../copy_s.py "$<TARGET_RUNTIME_DLLS:${PROJECT_NAME}>" ${TARGET_DIR}
)


