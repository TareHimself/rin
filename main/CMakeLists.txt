cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
include(${CMAKE_CURRENT_LIST_DIR}/../shared/cmake/utils.cmake)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
set(MODULES_DIR ${CMAKE_CURRENT_LIST_DIR}/../modules)

macro(LinkRinModule IN_MODULE_NAME)
    add_subdirectory(${MODULES_DIR}/${IN_MODULE_NAME} ${IN_MODULE_NAME})
    LinkToExecutable(rin rin${IN_MODULE_NAME})
endmacro()
#set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
#set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
#set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
project(rinMain VERSION "1.0.0" DESCRIPTION "")

file(GLOB_RECURSE SOURCE_FILES "${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp" "${CMAKE_CURRENT_SOURCE_DIR}/src/*.hpp" )

add_executable(rin ${SOURCE_FILES})

LinkRinModule("Core")
LinkRinModule("Window")
LinkRinModule("Graphics")
LinkRinModule("Widgets")
LinkRinModule("Audio")

CopyRuntimeDlls("rin" "rin")
CopyResourcesTo("rin")

