cmake_minimum_required(VERSION 3.18 FATAL_ERROR)
set(CMAKE_CXX_STANDARD 20)
project(ui)
set(EXECUTABLE_NAME "UI")
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR})
file(GLOB S_FILES ${CMAKE_CURRENT_SOURCE_DIR}/src/*.hpp ${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp)
add_executable(${EXECUTABLE_NAME} ${S_FILES})

add_subdirectory(../ vengine)

if(MSVC)
 target_compile_options(${EXECUTABLE_NAME} PRIVATE "/MP")
endif()

target_link_libraries(${EXECUTABLE_NAME} vengine)

add_custom_command ( TARGET ${EXECUTABLE_NAME} POST_BUILD
    COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/../copy_s.py "$<TARGET_RUNTIME_DLLS:vengine>" $<TARGET_FILE_DIR:${EXECUTABLE_NAME}>
)
add_custom_command ( TARGET ${EXECUTABLE_NAME} POST_BUILD
    COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/../copy_s.py "${CMAKE_CURRENT_SOURCE_DIR}/../ext/bass_main/bin/bass.dll" $<TARGET_FILE_DIR:${EXECUTABLE_NAME}>
)
#install(TARGETS libwave DESTINATION bin)