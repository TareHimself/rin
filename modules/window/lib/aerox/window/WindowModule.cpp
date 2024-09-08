#include <GLFW/glfw3.h>
#include "aerox/window/WindowModule.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/window/Window.hpp"
namespace aerox::window
{
    std::string WindowModule::GetName()
    {
        return "Window Module";
    }

    void WindowModule::Startup(GRuntime* runtime)
    {
        glfwInit();
        runtime->onTick->Add([](double _)
        {
            glfwPollEvents();
        });
    }

    void WindowModule::Shutdown(GRuntime* runtime)
    {
        glfwTerminate();
    }

    bool WindowModule::IsDependentOn(Module* module)
    {
        return false;
    }

    Shared<Window> WindowModule::Create(const std::string& name, int width, int height,
        const WindowCreateOptions& options)
    {
        options.Apply();
    
        if (const auto win = glfwCreateWindow(width, height, name.c_str(), nullptr, nullptr))
        {
            return newShared<Window>(win);
        }

        return {};
    }
}
