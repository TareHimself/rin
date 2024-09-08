cmake_minimum_required(VERSION 3.5 FATAL_ERROR)
include(${CMAKE_CURRENT_LIST_DIR}/../../shared/cmake/utils.cmake)
SetDefaults("aerox" "core")

project(${MODULE_NAME} VERSION "1.0.0" DESCRIPTION "The core lib for aerox")

SetupProject()
GetGlm(1.0.0)
GetFmt(10.2.1)