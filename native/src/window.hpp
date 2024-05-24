#pragma once
#include "macro.hpp"
#define GLFW_INCLUDE_VULKAN
#include <GLFW/glfw3.h>

EXPORT bool windowSubsystemStart();

EXPORT void windowSubsystemStop();

EXPORT void* windowCreate(int width, int height, const char* name,void* parent);

EXPORT void windowDestroy(void* window);

EXPORT void windowSubsystemPollEvents();

using GlfwKeyCallback = void(__stdcall *)(GLFWwindow* window, int key, int scancode, int action, int mods);
using GlfwCursorPosCallback = void(__stdcall *)(GLFWwindow* window, double x, double y);
using GlfwMouseButtonCallback = void(__stdcall *)(GLFWwindow* window, int button, int action, int mods);
using GlfwWindowFocusCallback = void(__stdcall *)(GLFWwindow* window, int focused);
using GlfwScrollCallback = void(__stdcall *)(GLFWwindow* window, const double dx, const double dy);
using GlfwWindowSizeCallback = void(__stdcall *)(GLFWwindow* window, const int width, const int height);
using GlfwWindowCloseCallback = void(__stdcall *)(GLFWwindow* window);
using GlfwCharCallback =  void(__stdcall *)(GLFWwindow* window, unsigned int codepoint, int mods);

EXPORT void windowSetCallbacks(void* window, const GlfwKeyCallback keyCallback,
                                 const GlfwCursorPosCallback cursorPosCallback,
                                 const GlfwMouseButtonCallback mouseButtonCallback,
                                 const GlfwWindowFocusCallback windowFocusCallback,
                                 const GlfwScrollCallback scrollCallback,
                                 const GlfwWindowSizeCallback windowSizeCallback,
                                 const GlfwWindowCloseCallback windowCloseCallback,
                                 const GlfwCharCallback charCallback);


EXPORT uintptr_t windowCreateSurface(void * instance,void * window);

EXPORT void * windowGetExtensions(unsigned int * length);

EXPORT void windowGetMousePosition(void* window, double * x,double * y);

EXPORT void windowGetPixelSize(void* window, int * x,int * y);
