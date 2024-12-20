cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
include(FetchContent)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
include(${CMAKE_CURRENT_SOURCE_DIR}/utils.cmake)

set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
# include(ExternalProject)

project(rin LANGUAGES CXX VERSION 1.0.0 DESCRIPTION "A graphics engine")
set(RIN_BUILD_DEV ${PROJECT_IS_TOP_LEVEL})
set(SRC_DIR ${CMAKE_CURRENT_LIST_DIR}/src)

file(GLOB_RECURSE HEADER_FILES "${CMAKE_CURRENT_SOURCE_DIR}/include/*.h")
file(GLOB_RECURSE SOURCE_FILES "${CMAKE_CURRENT_SOURCE_DIR}/lib/*.cpp")

add_library(${PROJECT_NAME} SHARED ${HEADER_FILES} ${SOURCE_FILES})
SOURCE_GROUP(rin FILES ${HEADER_FILES} ${SOURCE_FILES})
target_include_directories(${PROJECT_NAME} PUBLIC 
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
    $<INSTALL_INTERFACE:include>
)


FetchContent_Declare(glm
    GIT_REPOSITORY https://github.com/g-truc/glm
    GIT_TAG 1.0.0
    GIT_SHALLOW 1
)
# FetchContent_Declare(sdl
#     GIT_REPOSITORY https://github.com/libsdl-org/SDL
#     GIT_TAG preview-3.1.6
#     GIT_SHALLOW 1
# )
FetchContent_Declare(glfw
    GIT_REPOSITORY https://github.com/glfw/glfw
    GIT_TAG 3.4
    GIT_SHALLOW 1
)
FetchContent_Declare(vkma
    GIT_REPOSITORY https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator
    GIT_TAG v3.1.0
    GIT_SHALLOW 1
)
FetchContent_Declare(vkb
    GIT_REPOSITORY https://github.com/charles-lunarg/vk-bootstrap
    GIT_TAG v1.3.283
    GIT_SHALLOW 1
)

set(BUILD_SHARED_LIBS ON)
set(GLM_BUILD_TESTS OFF)
set(GLFW_BUILD_EXAMPLES OFF)
set(VK_BOOTSTRAP_TEST OFF)
set(GLFW_BUILD_TESTS OFF)
set(GLFW_BUILD_DOCS OFF)
set(SDL_TEST_LIBRARY OFF)
set(SDL_EXAMPLES OFF)
set(SDL_SHARED ON)
FetchContent_MakeAvailable(glm vkma vkb glfw)

find_package(Vulkan REQUIRED)
target_link_libraries(${PROJECT_NAME} PUBLIC glm::glm glfw Vulkan::Vulkan vk-bootstrap::vk-bootstrap GPUOpen::VulkanMemoryAllocator)
# target_link_libraries(${PROJECT_NAME} glfw)
GetSlang(${PROJECT_NAME} "2024.17")
GetStb(${PROJECT_NAME} "master")
GetBass()

add_custom_command ( TARGET ${PROJECT_NAME} POST_BUILD
    COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/copy_s.py "$<TARGET_RUNTIME_DLLS:${PROJECT_NAME}>" $<TARGET_FILE_DIR:${PROJECT_NAME}>
)

if(MSVC)
    target_compile_options(${PROJECT_NAME} PRIVATE "/MP")
endif()

if(EXISTS ${CMAKE_CURRENT_SOURCE_DIR}/resources)
    
    set(RIN_RESOURCES ${RIN_RESOURCES} ${CMAKE_CURRENT_SOURCE_DIR}/resources)
    install(
      DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/resources/
      DESTINATION bin/resources
    )
    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND python ${CMAKE_CURRENT_LIST_DIR}/copy_s.py "${RIN_RESOURCES}" "$<TARGET_FILE_DIR:${PROJECT_NAME}>"
    )
endif()

if(RIN_BUILD_DEV)
    file(GLOB_RECURSE DEV_HEADER_FILES "${CMAKE_CURRENT_SOURCE_DIR}/dev/*.h")
    file(GLOB_RECURSE DEV_SOURCE_FILES "${CMAKE_CURRENT_SOURCE_DIR}/dev/*.cpp")
    
    message(STATUS "FILES ${DEV_HEADER_FILES} ${DEV_SOURCE_FILES}")
    add_executable(dev ${DEV_HEADER_FILES} ${DEV_SOURCE_FILES})
    target_link_libraries(dev ${PROJECT_NAME})
    #target_include_directories(dev PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/include)
    target_include_directories(dev PRIVATE $<TARGET_PROPERTY:rin,INTERFACE_INCLUDE_DIRECTORIES>)
    SOURCE_GROUP(dev FILES ${HEADER_FILES} ${SOURCE_FILES})
endif()



