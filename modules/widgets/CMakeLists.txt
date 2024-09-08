cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
include(${CMAKE_CURRENT_LIST_DIR}/../../shared/cmake/utils.cmake)
SetDefaults("aerox" "widgets")

project(${MODULE_NAME} VERSION "1.0.0" DESCRIPTION "The widget lib for aerox")

SetupProject()
target_compile_definitions(${PROJECT_NAME} PUBLIC -DVULKAN_HPP_DISPATCH_LOADER_DYNAMIC=1)
AddModuleToModule(${CMAKE_CURRENT_LIST_DIR}/../graphics "aerox_graphics")


