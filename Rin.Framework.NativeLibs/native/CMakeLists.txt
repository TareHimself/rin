cmake_minimum_required(VERSION 3.10 FATAL_ERROR)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
set(BUILD_SHARED_LIBS ON)

include(FetchContent)

project(Rin.Framework.Native LANGUAGES C CXX VERSION 1.0.0 DESCRIPTION "Native abstractions for the Rin.Framework")
set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)
file(GLOB_RECURSE SOURCE_FILES "${SRC_DIR}/*.cpp" "${SRC_DIR}/*.hpp")

option(RIN_BUILD_TEST_MAIN "Build a test main file for debugging" OFF)

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})

target_include_directories(${PROJECT_NAME} PUBLIC ${SRC_DIR})

find_package(webmdx REQUIRED)
find_package(msdfgen REQUIRED)
target_link_libraries(${PROJECT_NAME} webmdx::webmdx msdfgen::msdfgen)

add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy "$<TARGET_RUNTIME_DLLS:${PROJECT_NAME}>;$<TARGET_FILE:${PROJECT_NAME}>" $<TARGET_FILE_DIR:${PROJECT_NAME}>
        COMMAND_EXPAND_LISTS
)

if(BUILD_SHARED_LIBS)
    target_compile_definitions(${PROJECT_NAME} PRIVATE RIN_NATIVE_PRODUCER)
endif()


