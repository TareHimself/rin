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

GetGlfw(3.3.8 GLFW_DIR)
list(APPEND CMAKE_PREFIX_PATH ${GLFW_DIR}/lib/cmake)

GetGlm(0.9.9.8 GLM_DIR)
list(APPEND CMAKE_PREFIX_PATH ${GLM_DIR}/cmake)

GetReactPhys(master REACT_PHYS_DIR)
list(APPEND CMAKE_PREFIX_PATH ${REACT_PHYS_DIR}/lib/cmake)


find_package(Vulkan REQUIRED)
find_package(glm REQUIRED)
find_package(glfw3 REQUIRED)
find_package(ReactPhysics3D REQUIRED)


target_include_directories(${PROJECT_NAME} PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/src ${Vulkan_INCLUDE_DIRS} ${GLFW_DIR}/include ${GLM_DIR}/glm ${REACT_PHYS_DIR}/include)

file(GLOB GLFW_LIB "${GLFW_DIR}/lib/*.lib")
file(GLOB REACT_PHYS_LIB "${REACT_PHYS_DIR}/lib/*.lib")

target_link_directories(${PROJECT_NAME} PUBLIC ${Vulkan_LIBRARIES} ${GLFW_LIB} ${REACT_PHYS_LIB})

target_link_libraries(${PROJECT_NAME} Vulkan::Vulkan glfw ReactPhysics3D::ReactPhysics3D)



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


