#include "window.hpp"

#include <vector>

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
                        const GlfwRefreshCallback refreshCallback
                        //,const GlfwDropCallback dropCallback
                        )
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

    //glfwSetDropCallback(window,dropCallback);
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

void windowGetWindowPosition(GLFWwindow* window, int* x, int* y)
{
    glfwGetWindowPos(window,x,y);
}

void windowSetWindowPosition(GLFWwindow* window, int x, int y)
{
    glfwSetWindowPos(window,x,y);
}

void windowSetWindowSize(GLFWwindow* window, int x, int y)
{
    glfwSetWindowSize(window,x,y);
}

void windowSetWindowTitle(GLFWwindow* window, const char* title)
{
    glfwSetWindowTitle(window,title);
}

void windowSetFullScreen(GLFWwindow* window, int fullscreen)
{
    if(fullscreen == 1)
    {
        if(glfwGetWindowMonitor(window))
        {
            return;
        }
        
        int numMonitors = 0;
        auto monitors = glfwGetMonitors(&numMonitors);
        int midpointX,midpointY;
        {
            int x,y,width,height;
            glfwGetWindowPos(window,&x,&y);
            glfwGetWindowSize(window,&width,&height);
            midpointX = x + (width / 2);
            midpointY = y + (height / 2);
        }
        for(auto i = 0; i < numMonitors; i++)
        {
            auto monitor = monitors[i];
            int x,y,width,height;
            glfwGetMonitorWorkarea(monitor,&x,&y,&width,&height);
            if(x <= midpointX && y <= midpointY && midpointX <= (x + width) && midpointY <= (y + height))
            {
                glfwSetWindowMonitor(window,monitor,0,0,width,height,GLFW_DONT_CARE);
                return;
            }
        }
    }
    else
    {
        if(auto monitor = glfwGetWindowMonitor(window))
        {
            int x,y,width,height;
            glfwGetMonitorWorkarea(monitor,&x,&y,&width,&height);
            width /= 2;
            height /= 2;
            x += width / 2;
            y += height / 2;
            glfwSetWindowMonitor(window,nullptr,x,y,width,height,GLFW_DONT_CARE);
        }
    }
}

int windowGetFullScreen(GLFWwindow* window)
{
    if(auto monitor = glfwGetWindowMonitor(window))
    {
        return 1;
    }
    return 0;
}
