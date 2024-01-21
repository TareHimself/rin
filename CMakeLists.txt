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

file(GLOB_RECURSE INCLUDE_FILES "src/vengine/*.hpp" )

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES} ${INCLUDE_FILES})

set(VENGINE_THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)

target_include_directories(${PROJECT_NAME} PUBLIC  ${VENGINE_THIRD_PARTY_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/src)

GetBuildExt(VENGINE_BUILD_EXT)
find_package(Vulkan REQUIRED)
target_link_libraries(${PROJECT_NAME} Vulkan::Vulkan)

GetSDL(main)

GetGlm(0.9.9.8)

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

