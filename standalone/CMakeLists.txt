cmake_minimum_required(VERSION 3.18 FATAL_ERROR)
set(CMAKE_CXX_STANDARD 20)
project(standalone)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR})
file(GLOB S_FILES ${CMAKE_CURRENT_SOURCE_DIR}/src/*.hpp ${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp)
add_executable(Standalone ${S_FILES})


add_custom_target(ReflectTestHeaders
    COMMAND python ${CMAKE_CURRENT_LIST_DIR}/../meta/main.py --source ${CMAKE_CURRENT_SOURCE_DIR}/src -o ${CMAKE_CURRENT_LIST_DIR}/src -c ${CMAKE_CURRENT_LIST_DIR}/../meta/config.json
    WORKING_DIRECTORY ${CMAKE_CURRENT_LIST_DIR}/../meta
    COMMENT "Reflect Test Headers"
)

add_dependencies(Standalone ReflectTestHeaders)

add_subdirectory(../ aerox)

if(MSVC)
 target_compile_options(Standalone PRIVATE "/MP")
endif()

target_link_libraries(Standalone aerox)

add_custom_command ( TARGET Standalone POST_BUILD
    COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/../copy_s.py "$<TARGET_RUNTIME_DLLS:aerox>" $<TARGET_FILE_DIR:Standalone>
)
add_custom_command ( TARGET Standalone POST_BUILD
    COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/../copy_s.py "${CMAKE_CURRENT_SOURCE_DIR}/../ext/bass_main/bin/bass.dll" $<TARGET_FILE_DIR:Standalone>
)
#install(TARGETS libwave DESTINATION bin)