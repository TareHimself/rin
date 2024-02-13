cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
include(${CMAKE_CURRENT_SOURCE_DIR}/utils.cmake)
# include(ExternalProject)

project(vengine VERSION 1.0.0 DESCRIPTION "Basic vulkan rendering engine")
set(VULKAN_HPP_VERSION 1.3.271)

set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
# Build a shared library named after the project from the files in `src/`
file(GLOB_RECURSE SOURCE_FILES "lib/vengine/*.cpp" )

file(GLOB_RECURSE INCLUDE_FILES "include/vengine/*.hpp" )

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES} ${INCLUDE_FILES})

set(VENGINE_THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
set(VENGINE_RUNTIME_DLLS "")
set(VENGINE_LIBS "")
target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
    $<INSTALL_INTERFACE:include> 
)

#   target_include_directories(
#     ${PROJECT_NAME}
#     PUBLIC
#     $<BUILD_INTERFACE:${RESULT_DIR}/include>
#     $<INSTALL_INTERFACE:include> 
#   )

#   install(
#     DIRECTORY ${RESULT_DIR}/include/
#     DESTINATION include/
#   )
GetBuildExt(VENGINE_BUILD_EXT)
find_package(Vulkan REQUIRED)
target_link_libraries(${PROJECT_NAME} PUBLIC Vulkan::Vulkan)


GetReflect(main)

GetGlfw(3.3.9)

GetBass(main)

GetGlm(1.0.0)

GetPugiXml(v1.14)

GetReactPhys(master)

GetVkBootstrap(v1.3.273)

GetVulkanMemoryAllocator(v3.0.1)#v3.0.1-3)

GetGLSL(14.0.0)

GetSpirvCross(vulkan-sdk-1.3.275.0)

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

GetFreeType(VER-2-13-2)

add_custom_target(ReflectEngineHeaders
    COMMAND python ${CMAKE_CURRENT_LIST_DIR}/python/reflect.py --source ${CMAKE_CURRENT_SOURCE_DIR}/include/vengine --output ${CMAKE_CURRENT_SOURCE_DIR}/include/generated
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    COMMENT "Reflect vengine headers"
)

add_dependencies(${PROJECT_NAME} ReflectEngineHeaders)

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
)

install(
    EXPORT vengine-targets
    FILE vengineConfig.cmake
    NAMESPACE vengine::
    DESTINATION lib/cmake/vengine
)


