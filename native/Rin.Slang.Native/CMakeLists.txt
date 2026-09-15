cmake_minimum_required(VERSION 3.10 FATAL_ERROR)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
set(BUILD_SHARED_LIBS ON)
include(FetchContent)

macro(FindSlang TARGET_PROJECT SLANG_VERSION)
  set(DOWNLOAD_DIR ${CMAKE_CURRENT_BINARY_DIR})
  set(SLANG_OUTPUT_DIR ${DOWNLOAD_DIR}/slang)

  if(CMAKE_SYSTEM_PROCESSOR MATCHES "aarch64|arm64")
    set(SLANG_ARCH "aarch64")
  else()
    set(SLANG_ARCH "x86_64")
  endif()

  if(NOT EXISTS ${SLANG_OUTPUT_DIR})
        message(STATUS "Downloading Slang ${SLANG_VERSION}")
        set(DOWNLOADED_FILE ${DOWNLOAD_DIR}/slang.zip)
        set(SLANG_BASE_URL "https://github.com/shader-slang/slang/releases/download/v")
        if(WIN32)
            file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-windows-${SLANG_ARCH}.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
            file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            if (CMAKE_BUILD_TYPE STREQUAL "Debug")
              file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-windows-${SLANG_ARCH}-debug-info.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
              file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            endif()
        elseif(LINUX)
            file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-linux-${SLANG_ARCH}.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
            file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            if (CMAKE_BUILD_TYPE STREQUAL "Debug")
              file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-linux-${SLANG_ARCH}-debug-info.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
              file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            endif()
        elseif(APPLE)
            file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-macos-${SLANG_ARCH}.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
            file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            if (CMAKE_BUILD_TYPE STREQUAL "Debug")
              file(DOWNLOAD ${SLANG_BASE_URL}${SLANG_VERSION}/slang-${SLANG_VERSION}-macos-${SLANG_ARCH}-debug-info.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)
              file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${SLANG_OUTPUT_DIR})
            endif()
        endif()
  endif()
  file(GLOB SLANG_BINARIES
    "${SLANG_OUTPUT_DIR}/bin/*.*"
    "${SLANG_OUTPUT_DIR}/lib/*.dylib"
    "${SLANG_OUTPUT_DIR}/lib/*.so*"
    "${SLANG_OUTPUT_DIR}/lib/*.dll"
  )

  find_library(SLANG_LIBRARY
      NAMES slang gfx slang-rt
      HINTS "${SLANG_OUTPUT_DIR}/lib"
  )
  target_include_directories(${TARGET_PROJECT} PRIVATE ${SLANG_OUTPUT_DIR}/include)
  target_link_libraries(${TARGET_PROJECT} ${SLANG_LIBRARY})

  # $<TARGET_RUNTIME_DLLS:...> can't see these since SLANG_LIBRARY is a bare find_library()
  # path, not a proper IMPORTED target - copy the Slang runtime binaries next to the built
  # target explicitly so they land alongside it (and end up in the packaged nuspec source dir).
  add_custom_command(TARGET ${TARGET_PROJECT} POST_BUILD
      COMMAND ${CMAKE_COMMAND} -E copy_if_different ${SLANG_BINARIES} $<TARGET_FILE_DIR:${TARGET_PROJECT}>
      COMMAND_EXPAND_LISTS
  )
endmacro()

project(Rin.Slang.Native LANGUAGES C CXX VERSION 1.0.0 DESCRIPTION "Native Slang shader compiler bindings for Rin.Slang.Compiler")
set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)
file(GLOB_RECURSE SOURCE_FILES "${SRC_DIR}/*.cpp" "${SRC_DIR}/*.hpp")

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})

target_include_directories(${PROJECT_NAME} PUBLIC ${SRC_DIR})

FindSlang(${PROJECT_NAME} 2025.9.2)

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
