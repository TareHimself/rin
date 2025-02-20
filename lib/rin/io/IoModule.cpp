#include <ranges>

#include "rin/core/GRuntime.h"
#include "rin/io/IoModule.h"
#include "rin/io/GlfwWindow.h"
#include <GLFW/glfw3.h>
#include "rin/io/GlfwWindow.h"
namespace rin::io
{
    void IoModule::Tick(double delta)
    {
        glfwPollEvents();
    }

    IoModule* IoModule::Get()
    {
        return GRuntime::Get()->GetModule<IoModule>();
    }

    Window * IoModule::CreateWindow(const Vec2<int>& size, const std::string& name,
                                           const std::optional<Window::CreateOptions>& options,Window * parent)
    {
        glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
        const auto createOptions = options.value_or(Window::CreateOptions{});
        glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
        glfwWindowHint(GLFW_RESIZABLE, createOptions.resizable ? GLFW_TRUE : GLFW_FALSE);
        glfwWindowHint(GLFW_VISIBLE, createOptions.visible ? GLFW_TRUE : GLFW_FALSE);
        glfwWindowHint(GLFW_DECORATED, createOptions.decorated ? GLFW_TRUE : GLFW_FALSE);
        glfwWindowHint(GLFW_FLOATING, createOptions.floating ? GLFW_TRUE : GLFW_FALSE);
        glfwWindowHint(GLFW_MAXIMIZED, createOptions.maximized ? GLFW_TRUE : GLFW_FALSE);
        //glfwWindowHint(GLFW_CENTER_CURSOR, createOptions.cursorCentered ? GLFW_TRUE : GLFW_FALSE);

        if(auto window = glfwCreateWindow(size.x,size.y,name.c_str(),nullptr,nullptr))
        {
            auto sharedWindow = shared<GlfwWindow>(window);
            
            _windows.emplace(window,sharedWindow);
            
            if(const auto parentAsSdlWindow = dynamic_cast<GlfwWindow *>(parent))
            {
                sharedWindow->SetParent(parentAsSdlWindow);
            }

            sharedWindow->onDisposed->Add([window,this]
            {
                if(const auto found = _windows.find(window); found != _windows.end())
                {
                    onWindowDestroyed->Invoke(found->second.get());
                    _windows.erase(found);
                }
            });

            onWindowCreated->Invoke(sharedWindow.get());
            return sharedWindow.get();
        }

        return {};
    }

    void IoModule::OnDispose()
    {
        for (auto windows = _windows; const auto& window : windows | std::views::values)
        {
            if(!window->GetParent())
            {
                window->Dispose();
            }
        }

        _windows.clear();
    }

    void IoModule::Startup(GRuntime* runtime)
    {
        glfwInit();
    }

    void IoModule::Shutdown(GRuntime* runtime)
    {
        glfwTerminate();
        _mainTaskRunner.StopAll();
    }

    std::string IoModule::GetName()
    {
        return "IO";
    }

    bool IoModule::IsDependentOn(Module* module)
    {
        return false;
    }

    void IoModule::RegisterRequiredModules(GRuntime* runtime)
    {
        
    }

    TaskRunner& IoModule::GetTaskRunner()
    {
        return _mainTaskRunner;
    }
}
