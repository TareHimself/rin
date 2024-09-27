#include <SDL3/SDL_init.h>

#include "rin/window/WindowModule.hpp"
#include "rin/core/GRuntime.hpp"
#include "rin/window/Window.hpp"
std::string WindowModule::GetName()
{
    return "Window Module";
}

void WindowModule::Startup(GRuntime* runtime)
{
    SDL_Init(SDL_INIT_VIDEO | SDL_INIT_EVENTS);
        
    runtime->onTick->Add([this](double _)
    {
        SDL_Event event{};
        while(SDL_PollEvent(&event))
        {
            auto windowId = SDL_GetWindowID(SDL_GetWindowFromEvent(&event));
            if(auto target = _windows.contains(windowId) ? _windows[windowId] : Shared<Window>{})
            {
                target->NotifyEvent(event);
            }
        }
    });
}

void WindowModule::Shutdown(GRuntime* runtime)
{
    SDL_Quit();
}

bool WindowModule::IsDependentOn(RinModule* module)
{
    return false;
}

Shared<Window> WindowModule::Create(const std::string& name, int width, int height,
    const WindowCreateOptions& options)
{
    if (const auto win = SDL_CreateWindow(name.c_str(),width, height,options.Apply()))
    {
        auto newWindow = newShared<Window>(win);
        auto id = SDL_GetWindowID(win);
        newWindow->onDisposed->Add([this,id](Window* _)
        {
            _windows.erase(id);
        });
        _windows.emplace(id,newWindow);
        return newWindow;
    }

    return {};
}
