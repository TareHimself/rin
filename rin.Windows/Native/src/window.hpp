#pragma once
#include "macro.hpp"
#include <GLFW/glfw3.h>


struct WindowCreateOptions
{
    bool resizable = true;
    
    bool visible = true;
    
    bool decorated = true;
    
    bool focused = true;
    
    bool floating = false;
    
    bool maximized = false;
    
    bool cursorCentered = false;
};

EXPORT bool windowSubsystemStart();

EXPORT void windowSubsystemStop();

EXPORT void* windowCreate(int width, int height, const char* name,const WindowCreateOptions * options);

EXPORT void windowDestroy(GLFWwindow* window);

EXPORT void windowSubsystemPollEvents();

using GlfwKeyCallback = void(__stdcall *)(GLFWwindow* window, int key, int scancode, int action, int mods);
using GlfwCursorPosCallback = void(__stdcall *)(GLFWwindow* window, double x, double y);
using GlfwMouseButtonCallback = void(__stdcall *)(GLFWwindow* window, int button, int action, int mods);
using GlfwWindowFocusCallback = void(__stdcall *)(GLFWwindow* window, int focused);
using GlfwScrollCallback = void(__stdcall *)(GLFWwindow* window, const double dx, const double dy);
using GlfwWindowSizeCallback = void(__stdcall *)(GLFWwindow* window, const int width, const int height);
using GlfwWindowCloseCallback = void(__stdcall *)(GLFWwindow* window);
using GlfwCharCallback =  void(__stdcall *)(GLFWwindow* window, unsigned int codepoint, int mods);

EXPORT void windowSetCallbacks(GLFWwindow* window, const GlfwKeyCallback keyCallback,
                                 const GlfwCursorPosCallback cursorPosCallback,
                                 const GlfwMouseButtonCallback mouseButtonCallback,
                                 const GlfwWindowFocusCallback windowFocusCallback,
                                 const GlfwScrollCallback scrollCallback,
                                 const GlfwWindowSizeCallback windowSizeCallback,
                                 const GlfwWindowCloseCallback windowCloseCallback,
                                 const GlfwCharCallback charCallback);


EXPORT void * windowGetExtensions(unsigned int * length);

EXPORT void windowGetMousePosition(GLFWwindow* window, double * x,double * y);

EXPORT void windowSetMousePosition(GLFWwindow* window, double x,double y);

EXPORT void windowGetPixelSize(GLFWwindow* window, int * x,int * y);

EXPORT void windowSetWindowPosition(GLFWwindow* window, int * x,int * y);

EXPORT void windowSetWindowSize(GLFWwindow* window, int * x,int * y);

EXPORT void windowSetWindowTitle(GLFWwindow* window,const char * title);
