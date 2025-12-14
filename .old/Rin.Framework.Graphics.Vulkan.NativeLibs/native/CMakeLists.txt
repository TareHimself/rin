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

macro(FindSlang TARGET_PROJECT SLANG_VERSION)
  set(SLANG_BINARIES_OUTPUT_DIR ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
  set(DOWNLOAD_DIR ${CMAKE_CURRENT_BINARY_DIR})
  set(SLANG_OUTPUT_DIR ${DOWNLOAD_DIR}/slang)
  
  if(NOT EXISTS ${SLANG_OUTPUT_DIR})
        set(DOWNLOADED_FILE ${DOWNLOAD_DIR}/slang.zip)
        set(SLANG_BASE_URL "https://github.com/shader-slang/slang/releases/download/v")
        if(WIN32)
            file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-windows-x86_64.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
            file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            if (CMAKE_BUILD_TYPE STREQUAL "Debug")
              file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-windows-x86_64-debug-info.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
              file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            endif()
        elseif(LINUX) 
            file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-linux-x86_64.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
            file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            if (CMAKE_BUILD_TYPE STREQUAL "Debug")
              file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-linux-x86_64-debug-info.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
              file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            endif()
        elseif(APPLE)
            message( FATAL_ERROR "Slang support not added for Apple." )
        endif()
  endif()
  file(GLOB SLANG_BINARIES
    "${SLANG_OUTPUT_DIR}/bin/*.*"
  )
  file(COPY ${SLANG_BINARIES} DESTINATION ${SLANG_BINARIES_OUTPUT_DIR})

  find_library(SLANG_LIBRARY
      NAMES slang gfx slang-rt
      HINTS "${SLANG_OUTPUT_DIR}/lib"
  )
  target_include_directories(${TARGET_PROJECT} PRIVATE ${SLANG_OUTPUT_DIR}/include)
  target_link_libraries(${TARGET_PROJECT} ${SLANG_LIBRARY})
endmacro()

project(Rin.Framework.Graphics.Vulkan.Native LANGUAGES C CXX VERSION 1.0.0 DESCRIPTION "Native abstractions for the Rin.Framework.Graphics.Vulkan")
set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)
file(GLOB_RECURSE SOURCE_FILES "${SRC_DIR}/*.cpp" "${SRC_DIR}/*.hpp")

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})

target_include_directories(${PROJECT_NAME} PUBLIC ${SRC_DIR})

find_package(Vulkan REQUIRED)
find_package(rwin REQUIRED)
find_package(vk-bootstrap)
target_link_libraries(${PROJECT_NAME} rwin::rwin Vulkan::Vulkan vk-bootstrap::vk-bootstrap GPUOpen::VulkanMemoryAllocator ${WAYLAND_LIBRARIES})
FindSlang(${PROJECT_NAME} 2025.9.2)
add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_RUNTIME_DLLS:${PROJECT_NAME}> $<TARGET_FILE_DIR:${PROJECT_NAME}>
        COMMAND_EXPAND_LISTS
)

if(BUILD_SHARED_LIBS)
    target_compile_definitions(${PROJECT_NAME} PRIVATE RIN_NATIVE_PRODUCER)
endif()


