cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
include(${CMAKE_CURRENT_LIST_DIR}/../../shared/cmake/utils.cmake)
SetDefaults("rin" "window")

project(${MODULE_NAME} VERSION "1.0.0" DESCRIPTION "The window lib for rin")

SetupProject()
AddModuleToModule(${CMAKE_CURRENT_LIST_DIR}/../core "rin_core")
GetSDL(main)

