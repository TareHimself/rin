#include "window.hpp"

bool windowSubsystemStart()
{
    return glfwInit() == GLFW_TRUE;
}

void windowSubsystemStop()
{
    glfwTerminate();
}


void* windowCreate(int width, int height, const char* name,const WindowCreateOptions* options)
{
    glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
    glfwWindowHint(GLFW_RESIZABLE, options->resizable ? GLFW_TRUE : GLFW_FALSE);
    glfwWindowHint(GLFW_VISIBLE, options->visible ? GLFW_TRUE : GLFW_FALSE);
    glfwWindowHint(GLFW_DECORATED, options->decorated ? GLFW_TRUE : GLFW_FALSE);
    glfwWindowHint(GLFW_FLOATING, options->floating ? GLFW_TRUE : GLFW_FALSE);
    glfwWindowHint(GLFW_MAXIMIZED, options->maximized ? GLFW_TRUE : GLFW_FALSE);
    glfwWindowHint(GLFW_CENTER_CURSOR, options->cursorCentered ? GLFW_TRUE : GLFW_FALSE);
    
    
    if (const auto win = glfwCreateWindow(width, height, name, nullptr, nullptr))
    {
        return win;
    }

    return nullptr;
}


void windowDestroy(GLFWwindow* window)
{
    glfwDestroyWindow(window);
}


void windowSubsystemPollEvents()
{
    glfwPollEvents();
}


void windowSetCallbacks(GLFWwindow* window, const GlfwKeyCallback keyCallback,
                        const GlfwCursorPosCallback cursorPosCallback,
                        const GlfwMouseButtonCallback mouseButtonCallback,
                        const GlfwWindowFocusCallback windowFocusCallback, const GlfwScrollCallback scrollCallback,
                        const GlfwWindowSizeCallback windowSizeCallback,
                        const GlfwWindowCloseCallback windowCloseCallback,
                        const GlfwCharCallback charCallback,
                        const GlfwMaximizedCallback maximizedCallback,
                        const GlfwRefreshCallback refreshCallback,
                        const GlfwDropCallback dropCallback)
{

    glfwSetKeyCallback(window, keyCallback);

    glfwSetCursorPosCallback(window, cursorPosCallback);

    glfwSetMouseButtonCallback(window, mouseButtonCallback);

    glfwSetWindowFocusCallback(window, windowFocusCallback);

    glfwSetScrollCallback(window, scrollCallback);

    glfwSetFramebufferSizeCallback(window, windowSizeCallback);

    glfwSetWindowCloseCallback(window, windowCloseCallback);

    glfwSetCharModsCallback(window, charCallback);

    glfwSetWindowMaximizeCallback(window,maximizedCallback);

    glfwSetWindowRefreshCallback(window,refreshCallback);

    glfwSetDropCallback(window,dropCallback);
}

void* windowGetExtensions(unsigned* length)
{
    return glfwGetRequiredInstanceExtensions(length);
}

void windowGetMousePosition(GLFWwindow* window, double* x, double* y)
{
    glfwGetCursorPos(window, x, y);
}

void windowSetMousePosition(GLFWwindow* window, double x,double y)
{
    glfwSetCursorPos(window,x,y);
}

void windowGetPixelSize(GLFWwindow* window, int* x, int* y)
{
    glfwGetFramebufferSize(window, x, y);
}

void windowSetWindowPosition(GLFWwindow* window, int* x, int* y)
{
    glfwSetWindowPos(window,*x,*y);
}

void windowSetWindowSize(GLFWwindow* window, int* x, int* y)
{
    glfwSetWindowSize(window,*x,*y);
}

void windowSetWindowTitle(GLFWwindow* window, const char* title)
{
    glfwSetWindowTitle(window,title);
}
