#include <ranges>

#include "rin/core/GRuntime.h"
#include "rin/io/IoModule.h"
#include "rin/io/SDLWindow.h"
#include "SDL3/SDL_events.h"
#include "SDL3/SDL.h"
#include "rin/io/SDLWindow.h"
namespace rin::io
{
    void IoModule::Tick(double delta)
    {
        SDL_Event e;
        while(SDL_PollEvent(&e))
        {
            auto window = SDL_GetWindowFromEvent(&e);
            if(_windows.contains(window))
            {
                if(const auto asSdlWindow = dynamicCast<SDLWindow>(_windows[window]))
                {
                    asSdlWindow->HandleEvent(e);
                }
            }
        }
    }

    IoModule* IoModule::Get()
    {
        return GRuntime::Get()->GetModule<IoModule>();
    }

    IWindow * IoModule::CreateWindow(const Vec2<int>& size, const std::string& name,
                                           const std::optional<IWindow::CreateOptions>& options,IWindow * parent)
    {
        SDL_WindowFlags flags = SDL_WINDOW_MOUSE_RELATIVE_MODE | SDL_WINDOW_VULKAN;
        const auto createOptions = options.value_or(IWindow::CreateOptions{});
        if(createOptions.resizable)
        {
            flags |= SDL_WINDOW_RESIZABLE;
        }
        if(!createOptions.visible)
        {
            flags |= SDL_WINDOW_HIDDEN;
        }
        // if(createOptions.focused)
        // {
        //     flags |= SDL_WINDOW_INPUT_FOCUS || SDL_WINDOW_MOUSE_FOCUS;
        // }
        if(createOptions.floating)
        {
            flags |= SDL_WINDOW_ALWAYS_ON_TOP;
        }
        if(createOptions.maximized)
        {
            flags |= SDL_WINDOW_MAXIMIZED;
        }
        if(createOptions.transparent)
        {
            flags |= SDL_WINDOW_TRANSPARENT;
        }
        
        if(auto window = SDL_CreateWindow(name.c_str(),size.x,size.y,flags))
        {
            auto sharedWindow = shared<SDLWindow>(window);
            
            _windows.emplace(window,sharedWindow);
            
            if(auto parentAsSdlWindow = dynamic_cast<SDLWindow *>(parent))
            {
                sharedWindow->SetParent(parentAsSdlWindow);
            }

            sharedWindow->onDisposed->Add([window,this]
            {
                if(const auto found = _windows.find(window); found != _windows.end())
                {
                    _windows.erase(found);
                }
            });
            
            return sharedWindow.get();
        }

        return {};
    }

    void IoModule::OnDispose()
    {
        for (auto windows = _windows; const auto window : windows | std::views::values)
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
        SDL_Init(SDL_INIT_VIDEO);
    }

    void IoModule::Shutdown(GRuntime* runtime)
    {
        SDL_Quit();
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
