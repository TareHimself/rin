cmake_minimum_required(VERSION 3.10 FATAL_ERROR)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
set(BUILD_SHARED_LIBS ON)
include(FetchContent)

FetchContent_Declare(fetch_vkma
       GIT_REPOSITORY https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator
       GIT_TAG v3.1.0
       GIT_SHALLOW 1
)

set(BUILD_SHARED_LIBS ON)
FetchContent_MakeAvailable(fetch_vkma)

project(Rin.Graphics.Vulkan.Native LANGUAGES C CXX VERSION 1.0.0 DESCRIPTION "Native abstractions for the Rin.Graphics.Vulkan")
set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)
file(GLOB_RECURSE SOURCE_FILES "${SRC_DIR}/*.cpp" "${SRC_DIR}/*.hpp")

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})

target_include_directories(${PROJECT_NAME} PUBLIC ${SRC_DIR})

find_package(Vulkan REQUIRED)
find_package(rwin REQUIRED)
find_package(vk-bootstrap)
target_link_libraries(${PROJECT_NAME} rwin::rwin Vulkan::Vulkan vk-bootstrap::vk-bootstrap GPUOpen::VulkanMemoryAllocator ${WAYLAND_LIBRARIES})
if(WIN32)
    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_RUNTIME_DLLS:${PROJECT_NAME}> $<TARGET_FILE_DIR:${PROJECT_NAME}>
            COMMAND_EXPAND_LISTS
    )
endif()

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
