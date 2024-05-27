cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
include(${CMAKE_CURRENT_SOURCE_DIR}/utils.cmake)

set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
# include(ExternalProject)

project(aerox.Font.Native VERSION 1.0.0 DESCRIPTION "Native abstractions for the Aerox Font")

set(THIRD_PARTY_DIR ${CMAKE_CURRENT_LIST_DIR}/ext)

# GetBass(main)
# GetGlm(1.0.0)

# GetTinyxml2(10.0.0)

# GetSimdJson(v3.6.3)
# GetArgparse(v3.0)
# GetFastGLTF(main)


set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)



file(GLOB_RECURSE SOURCE_FILES "${SRC_DIR}/*.cpp" "${SRC_DIR}/*.hpp")
message(STATUS "SOURCE FILES ${SOURCE_FILES}")

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})

target_include_directories(${PROJECT_NAME} PUBLIC ${SRC_DIR})

find_package(Vulkan REQUIRED)
target_link_libraries(${PROJECT_NAME} PUBLIC Vulkan::Vulkan)
GetMsdfGen(${PROJECT_NAME} v1.11)
SetDynamicLibraryDir(${PROJECT_NAME})

add_custom_command ( TARGET ${PROJECT_NAME} POST_BUILD
    COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/copy_s.py "$<TARGET_RUNTIME_DLLS:${PROJECT_NAME}>" ${TARGET_DIR}
)


