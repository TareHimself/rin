cmake_minimum_required(VERSION 3.18 FATAL_ERROR)

include(${CMAKE_CURRENT_SOURCE_DIR}/utils.cmake)
include(ExternalProject)

project(vengine VERSION 1.0.0 DESCRIPTION "Basic vulkan rendering engine")
set(VULKAN_HPP_VERSION 1.3.271)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
# Build a shared library named after the project from the files in `src/`
file(GLOB_RECURSE SOURCE_FILES "src/vengine/*.cpp" )

file(GLOB_RECURSE INCLUDE_FILES "src/vengine/*.hpp" )

add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES} ${INCLUDE_FILES})

set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)

GetSDL(release-2.28.5 SDL_DIR)
list(APPEND CMAKE_PREFIX_PATH ${SDL_DIR}/cmake)

GetGlm(0.9.9.8 GLM_DIR)
list(APPEND CMAKE_PREFIX_PATH ${GLM_DIR}/cmake)

GetReactPhys(master REACT_PHYS_DIR)
list(APPEND CMAKE_PREFIX_PATH ${REACT_PHYS_DIR}/lib/cmake)

GetVkBootstrap(v1.3.273 VK_BOOTSTRAP_DIR)

GetVulkanMemoryAllocator(v3.0.1 VK_MEMORY_ALLOCATOR_DIR)
list(APPEND CMAKE_PREFIX_PATH ${VK_MEMORY_ALLOCATOR_DIR})

GetGLSL(14.0.0 GLSL_DIR)
list(APPEND CMAKE_PREFIX_PATH ${GLSL_DIR}/lib/cmake)
list(APPEND CMAKE_PREFIX_PATH ${GLSL_DIR}/lib/cmake/glslang)

find_package(Vulkan REQUIRED)
find_package(glm REQUIRED)
find_package(SDL2 REQUIRED)
find_package(ReactPhysics3D REQUIRED)
find_package(glslang REQUIRED)
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


