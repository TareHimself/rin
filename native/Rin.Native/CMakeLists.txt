cmake_minimum_required(VERSION 3.25 FATAL_ERROR)

project(Rin.Native LANGUAGES C CXX VERSION 1.0.0 DESCRIPTION "Native abstractions for the Rin")

set(CMAKE_CXX_STANDARD 20)
set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)
file(GLOB_RECURSE SOURCE_FILES "${SRC_DIR}/*.cpp" "${SRC_DIR}/*.hpp")

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})

target_include_directories(${PROJECT_NAME} PUBLIC ${SRC_DIR})

find_package(webmdx REQUIRED)
find_package(msdfgen REQUIRED)
target_link_libraries(${PROJECT_NAME} PRIVATE webmdx::webmdx msdfgen::msdfgen-core)

target_compile_definitions(${PROJECT_NAME} PRIVATE RIN_NATIVE_PRODUCER)

# One folder with the module and the shared libraries it loads, on every system
install(TARGETS ${PROJECT_NAME} RUNTIME DESTINATION bin LIBRARY DESTINATION bin)
if(SILO_RUNTIME_LIBRARIES)
    install(FILES ${SILO_RUNTIME_LIBRARIES} DESTINATION bin)
endif()
