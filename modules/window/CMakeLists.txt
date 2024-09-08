cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
include(${CMAKE_CURRENT_LIST_DIR}/../../shared/cmake/utils.cmake)
SetDefaults("aerox" "window")

project(${MODULE_NAME} VERSION "1.0.0" DESCRIPTION "The window lib for aerox")

SetupProject()
AddModuleToModule(${CMAKE_CURRENT_LIST_DIR}/../core "aerox_core")
GetGlfw(3.4)

