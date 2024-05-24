#include "window.hpp"

bool windowSubsystemStart()
{
    return glfwInit() == GLFW_TRUE;
}

void windowSubsystemStop()
{
    glfwTerminate();
}


    void* windowCreate(int width, int height, const char* name,void* parent)
{
    glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
    if (const auto win = glfwCreateWindow(width, height, name, nullptr, static_cast<GLFWwindow*>(parent)))
    {
        return win;
    }

    return nullptr;
}


    void windowDestroy(void* window)
{
    glfwDestroyWindow(static_cast<GLFWwindow*>(window));
}


    void windowSubsystemPollEvents()
{
    glfwPollEvents();
}


    void windowSetCallbacks(void* window, const GlfwKeyCallback keyCallback,
                          const GlfwCursorPosCallback cursorPosCallback,
                          const GlfwMouseButtonCallback mouseButtonCallback,
                          const GlfwWindowFocusCallback windowFocusCallback, const GlfwScrollCallback scrollCallback,
                          const GlfwWindowSizeCallback windowSizeCallback,
                          const GlfwWindowCloseCallback windowCloseCallback,
                          const GlfwCharCallback charCallback)
{
    const auto asWin = static_cast<GLFWwindow*>(window);

    glfwSetKeyCallback(asWin, keyCallback);

    glfwSetCursorPosCallback(asWin, cursorPosCallback);

    glfwSetMouseButtonCallback(asWin, mouseButtonCallback);

    glfwSetWindowFocusCallback(asWin, windowFocusCallback);

    glfwSetScrollCallback(asWin, scrollCallback);

    glfwSetFramebufferSizeCallback(asWin, windowSizeCallback);

    glfwSetWindowCloseCallback(asWin, windowCloseCallback);

    glfwSetCharModsCallback(asWin,charCallback);
}

uintptr_t windowCreateSurface(void * instance, void* window)
{
    VkSurfaceKHR surf;

    const auto inst = reinterpret_cast<VkInstance>(instance);
    
    glfwCreateWindowSurface(inst,static_cast<GLFWwindow*>(window),nullptr,&surf);

    return reinterpret_cast<uintptr_t>(surf);
}

void * windowGetExtensions(unsigned* length)
{
    return glfwGetRequiredInstanceExtensions(length);
}

void windowGetMousePosition(void* window, double* x, double* y)
{
    glfwGetCursorPos(static_cast<GLFWwindow*>(window),x,y);
}

void windowGetPixelSize(void* window, int* x, int* y)
{
    glfwGetFramebufferSize(static_cast<GLFWwindow*>(window),x,y);
}
