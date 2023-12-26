cmake_minimum_required(VERSION 3.18 FATAL_ERROR)
set(CMAKE_CXX_STANDARD 20)

include(${CMAKE_CURRENT_SOURCE_DIR}/utils.cmake)
include(ExternalProject)

project(vengine VERSION 1.0.0 DESCRIPTION "Basic vulkan rendering engine")
set(VULKAN_HPP_VERSION 1.3.271)

set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
# Build a shared library named after the project from the files in `src/`
file(GLOB_RECURSE SOURCE_FILES "src/vengine/*.cpp" )

file(GLOB_RECURSE INCLUDE_FILES "src/vengine/*.hpp" )

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES} ${INCLUDE_FILES})
set(VENGINE_THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
GetBuildExt(VENGINE_BUILD_EXT)
find_package(Vulkan REQUIRED)

GetSDL(release-2.28.5 SDL_DIR)
find_package(SDL2 REQUIRED)

GetGlm(0.9.9.8 GLM_DIR)
find_package(glm REQUIRED)

GetReactPhys(master REACT_PHYS_DIR)
find_package(ReactPhysics3D REQUIRED)

GetVkBootstrap(v1.3.273 VK_BOOTSTRAP_DIR)

GetVulkanMemoryAllocator(v3.0.1 VK_MEMORY_ALLOCATOR_DIR)
list(APPEND CMAKE_PREFIX_PATH ${VK_MEMORY_ALLOCATOR_DIR})

GetGLSL(14.0.0 GLSL_DIR)
find_package(SPIRV-Tools-opt REQUIRED)
find_package(glslang CONFIG REQUIRED)

GetSpdLog(v1.12.0 SPD_LOG_DIR)
find_package(spdlog REQUIRED)

GetFmt(10.1.1 FMT_DIR)
find_package(fmt REQUIRED)

GetXXHash(v0.8.2 XXHASH_DIR)
find_package(xxHash REQUIRED)

GetMiniz(3.0.2 MINIZ_DIR)
find_package(miniz REQUIRED)

GetTinyObjLoader(release TINY_OBJ_LOADER_DIR)
find_package(tinyobjloader REQUIRED)

add_subdirectory(${VK_BOOTSTRAP_DIR} vk-bootstrap)


file(GLOB VK_BOOTSTRAP_INCLUDE "${VK_BOOTSTRAP_DIR}/*.h")

target_include_directories(${PROJECT_NAME} 
    PUBLIC 
    ${CMAKE_CURRENT_SOURCE_DIR}/src 
    ${Vulkan_INCLUDE_DIRS} 
    ${SDL2_INCLUDE_DIRS}
    ${GLM_DIR}/include 
    ${REACT_PHYS_DIR}/include 
    ${VK_BOOTSTRAP_INCLUDE} 
    ${VK_MEMORY_ALLOCATOR_DIR}
    ${GLSL_DIR}/include
    ${SPD_LOG_DIR}/include
    ${FMT_DIR}/include
    ${XXHASH_DIR}/include
    ${MINIZ_DIR}/include
    ${TINY_OBJ_LOADER_DIR}/include
)

# file(GLOB SDL2_LIB "${SDL_DIR}/lib/*")
# file(GLOB REACT_PHYS_LIB "${REACT_PHYS_DIR}/lib/*")
# file(GLOB VK_MEMORY_ALLOCATOR_LIB "${VK_MEMORY_ALLOCATOR_DIR}/lib/*")

# target_link_directories(${PROJECT_NAME} 
#     PUBLIC 
#     ${Vulkan_LIBRARIES} 
#     ${REACT_PHYS_LIB} 
#     ${VK_MEMORY_ALLOCATOR_LIB}
# )

file(GLOB VK_MEMORY_ALLOCATOR_LIB "${VK_MEMORY_ALLOCATOR_DIR}/lib/*")

file(GLOB SDL2_LIB "${SDL_DIR}/lib/*.lib")

target_link_directories(${PROJECT_NAME} 
    PUBLIC 
    ${VK_MEMORY_ALLOCATOR_LIB}
)

target_link_libraries(${PROJECT_NAME} 
    Vulkan::Vulkan 
    SDL2::SDL2
    SDL2::SDL2main
    ReactPhysics3D::ReactPhysics3D 
    vk-bootstrap::vk-bootstrap
    glslang::glslang 
    glslang::SPIRV 
    glslang::glslang-default-resource-limits
    spdlog::spdlog
    fmt::fmt
    xxHash::xxhash
    miniz::miniz
    tinyobjloader::tinyobjloader
)



# ExternalProject_Add(glfw
# GIT_REPOSITORY  https://github.com/glfw/glfw.git
# GIT_TAG         3.3.8
# SOURCE_DIR      
# )


# find_package(Vulkan REQUIRED)
# target_include_directories(${PROJECT_NAME} PUBLIC ${Vulkan_INCLUDE_DIRS})
# target_link_libraries(${PROJECT_NAME} Vulkan::Vulkan)

# # list(set glfw3_DIR ${VULKAN_HPP_PATH}/glfw/CMake)
# # message(STATUS "GLFW PREFIX ${VULKAN_HPP_PATH}/glfw/CMAKE")
# # find_package(glfw3 REQUIRED)
# # include_directories(${GLFW_INCLUDE_DIRS})
# # target_link_libraries(${PROJECT_NAME} ${GLFW_LIBRARIES})


# message(STATUS "VULKAN INCLUDE ${Vulkan_LIBRARIES}")


# set_target_properties(${PROJECT_NAME} PROPERTIES 
# VERSION ${PROJECT_VERSION}
# PUBLIC_HEADER ${INCLUDE_FLES}
# )

# set(LIBWAVE_INCLUDE_DIR ${CMAKE_CURRENT_SOURCE_DIR}/include PARENT_SCOPE)
# # install(TARGETS ${PROJECT_NAME}
# #     LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
# #     PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR})


