cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
include(${CMAKE_CURRENT_SOURCE_DIR}/utils.cmake)

# include(ExternalProject)

project(aerox VERSION 1.0.0 DESCRIPTION "Basic vulkan rendering engine")

set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
# Build a shared library named after the project from the files in `src/`
file(GLOB_RECURSE SOURCE_FILES "lib/aerox/*.cpp" )

file(GLOB_RECURSE INCLUDE_FILES "include/aerox/*.hpp" )

file(GLOB_RECURSE GENERATED_SOURCE_FILES "include/gen/*pp" )

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES} ${GENERATED_SOURCE_FILES} ${INCLUDE_FILES})

source_group(TREE ${CMAKE_CURRENT_SOURCE_DIR} FILES "${INCLUDE_FILES};${SOURCE_FILES}")

#target_compile_features(${PROJECT_NAME} PRIVATE cxx_std_20)

set(VENGINE_THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ext)
set(VENGINE_RUNTIME_DLLS "")
set(VENGINE_LIBS "")
target_include_directories(
        ${PROJECT_NAME}
        PUBLIC
        $<BUILD_INTERFACE:${VENGINE_THIRD_PARTY_DIR}>
        $<INSTALL_INTERFACE:include/ext>
)
target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
    $<INSTALL_INTERFACE:include> 
)

target_compile_definitions(${PROJECT_NAME} PUBLIC -DNOMINMAX -DGLM_FORCE_ALIGNED_GENTYPES -DGLM_ENABLE_EXPERIMENTAL)

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

list(APPEND CMAKE_PREFIX_PATH ${VENGINE_THIRD_PARTY_DIR})

# Externals
GetGlfw(3.3.9)
GetBass(main)
GetGlm(1.0.0)
GetReactPhys(master)
GetVkBootstrap(v1.3.277)
GetVulkanMemoryAllocator(master)#v3.0.1-3)
GetGLSL(14.0.0)
GetSpirvCross(main)
GetTinyxml2(10.0.0)
GetMsdfGen(v1.11)
GetFreeType(VER-2-13-2)
GetFmt(10.2.1)
GetVScript(v3.0.3)
GetXXHash(v0.8.2)
GetMiniz(3.0.2)
GetStb(master)
GetStdUUID(v1.2.3)
GetSimdJson(v3.6.3)
GetFastGLTF(main)
GetArgparse(v3.0)
GetJson(v3.11.3)
# Reflection
add_custom_target(ReflectEngineHeaders
    COMMAND python ${CMAKE_CURRENT_LIST_DIR}/python/reflect.py --source ${CMAKE_CURRENT_SOURCE_DIR}/include/aerox --output ${CMAKE_CURRENT_SOURCE_DIR}/include/generated
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    COMMENT "Reflect aerox headers"
)

add_dependencies(${PROJECT_NAME} ReflectEngineHeaders)

if(MSVC)
 target_compile_options(${PROJECT_NAME} PRIVATE "/MP")
endif()

install(
    DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/include/
    DESTINATION include/
)

install(
    TARGETS aerox
    EXPORT aerox-targets
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)

install(
    EXPORT aerox-targets
    FILE aeroxConfig.cmake
    NAMESPACE aerox::
    DESTINATION lib/cmake/aerox
)


