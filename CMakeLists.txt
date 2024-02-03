cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
include(${CMAKE_CURRENT_SOURCE_DIR}/utils.cmake)
# include(ExternalProject)

project(vengine VERSION 1.0.0 DESCRIPTION "Basic vulkan rendering engine")
set(VULKAN_HPP_VERSION 1.3.271)

set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
# Build a shared library named after the project from the files in `src/`
file(GLOB_RECURSE SOURCE_FILES "src/vengine/*.cpp" )

file(GLOB_RECURSE INCLUDE_FILES "include/vengine/*.hpp" )

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES} ${INCLUDE_FILES})

set(VENGINE_THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)

target_include_directories(${PROJECT_NAME} PUBLIC ${VENGINE_THIRD_PARTY_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/include)

GetBuildExt(VENGINE_BUILD_EXT)
find_package(Vulkan REQUIRED)
target_link_libraries(${PROJECT_NAME} PUBLIC Vulkan::Vulkan)


GetReflect(main)

GetSDL(main)

GetGlm(0.9.9.8)

GetPugiXml(v1.14)

GetReactPhys(master)

GetVkBootstrap(v1.3.273)

GetVulkanMemoryAllocator(v3.0.1)#v3.0.1-3)

GetGLSL(14.0.0)

GetSpirvCross(MoltenVK-1.1.5)

GetSpdLog(v1.12.0)

GetFmt(10.1.1)

GetXXHash(v0.8.2)

GetMiniz(3.0.2)

GetStdUUID(v1.2.3)

GetSimdJson(v3.6.3)

GetFastGLTF(main)

GetOpenCV(4.9.0)

GetAngelScript(2.36.1)

GetArgparse(v3.0)

set(ReflectionExec "VEngineHeaderGen")
set(Reflection_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/reflection/main.cpp)
add_executable(${ReflectionExec} ${Reflection_SOURCES})
target_link_libraries(${ReflectionExec} PUBLIC reflect::reflect)
target_link_libraries(${ReflectionExec} PUBLIC argparse::argparse)
target_include_directories(${ReflectionExec}
    PRIVATE
    "$<TARGET_PROPERTY:${PROJECT_NAME},INTERFACE_INCLUDE_DIRECTORIES>"
)

add_custom_target(ReflectHeaders
    COMMAND ${ReflectionExec} -s ${CMAKE_CURRENT_SOURCE_DIR}/include -o ${CMAKE_CURRENT_SOURCE_DIR}/include/generated
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    COMMENT "Reflect vengine headers"
    SOURCES ${Reflection_SOURCES}
)

add_dependencies(${PROJECT_NAME} ReflectHeaders)

# add_executable(ReflectGen ${CMAKE_CURRENT_SOURCE_DIR}/reflect.cc)
# target_link_libraries(ReflectGen PUBLIC reflect::reflect)

# add_custom_command(
#   TARGET ${PROJECT_NAME}
#   COMMAND ReflectGen
#   COMMENT "Reflecting Engine Files"
# )

install(
    DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/include/
    DESTINATION include/
)

install(
    TARGETS vengine
    EXPORT vengine-targets
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
    INCLUDES DESTINATION include
)

install(
    EXPORT vengine-targets
    FILE vengineConfig.cmake
    NAMESPACE vengine::
    DESTINATION lib/cmake/vengine
)


