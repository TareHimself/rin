cmake_minimum_required(VERSION 3.10 FATAL_ERROR)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
set(BUILD_SHARED_LIBS ON)

include(FetchContent)

project(Rin.Audio.Miniaudio.Native LANGUAGES C CXX VERSION 1.0.0 DESCRIPTION "")
set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)
file(GLOB_RECURSE SOURCE_FILES "${SRC_DIR}/*.cpp" "${SRC_DIR}/*.hpp")

# option(RIN_BUILD_TEST_MAIN "Build a test main file for debugging" OFF)

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})

target_include_directories(${PROJECT_NAME} PUBLIC ${SRC_DIR})

find_package(miniaudio REQUIRED)
target_link_libraries(${PROJECT_NAME} PRIVATE miniaudio::miniaudio)

add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy "$<TARGET_RUNTIME_DLLS:${PROJECT_NAME}>;$<TARGET_FILE:${PROJECT_NAME}>" $<TARGET_FILE_DIR:${PROJECT_NAME}>
        COMMAND_EXPAND_LISTS
)

if(BUILD_SHARED_LIBS)
    target_compile_definitions(${PROJECT_NAME} PRIVATE RIN_NATIVE_PRODUCER)
endif()

find_package(Python3 REQUIRED COMPONENTS Interpreter)
add_custom_command(
    TARGET ${PROJECT_NAME}
    POST_BUILD
    COMMAND Python3::Interpreter 
    ${CMAKE_CURRENT_LIST_DIR}/../make_nuspec_cli.py 
    --name ${PROJECT_NAME} 
    --version ${PROJECT_VERSION} 
    --author "TareHimself" 
    --source $<TARGET_FILE_DIR:${PROJECT_NAME}> 
    --output ${CMAKE_CURRENT_LIST_DIR}/specs/${CMAKE_BUILD_TYPE}.nuspec
)


