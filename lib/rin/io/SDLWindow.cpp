#include "rin/io/SDLWindow.h"

#include <SDL3/SDL_mouse.h>
#include <SDL3/SDL_vulkan.h>
#include "rin/io/IoModule.h"
#include "SDL3/SDL_video.h"
namespace rin::io
{
    SDLWindow::SDLWindow(SDL_Window* ptr)
    {
        _ptr = ptr;
    }
    
    void SDLWindow::OnDispose()
    {
        for (const auto child : _children)
        {
            child->Dispose();
        }

        _children.clear();
        
        onDisposed->Invoke();
    }

    Vec2<double> SDLWindow::GetCursorPosition()
    {
        // Vec2<int> position{0};
        //
        // SDL_GetWindowPosition(GetSdlWindow(),&position.x,&position.y);
        // return static_cast<Vec2<double>>(position);
        return {0};
    }

    void SDLWindow::SetCursorPosition(const Vec2<double>& position)
    {
        
    }

    void SDLWindow::SetFullscreen(bool state)
    {
    }

    Vec2<uint32_t> SDLWindow::GetPixelSize()
    {
        return {0};
    }

    IWindow * SDLWindow::CreateChild(const Vec2<int>& size, const std::string& name,
        const std::optional<CreateOptions>& options)
    {
        auto child = IoModule::Get()->CreateWindow(size,name,options,this);
        _children.push_back(child);
        return child;
    }

    IWindow * SDLWindow::GetParent()
    {
        return _parent;
    }

    bool SDLWindow::IsFocused()
    {
        auto flags = SDL_GetWindowFlags(GetSdlWindow());
        return (flags && SDL_WINDOW_INPUT_FOCUS) == 1 || (flags && SDL_WINDOW_MOUSE_FOCUS) == 1;
    }

    bool SDLWindow::IsFullscreen()
    {
        auto flags = SDL_GetWindowFlags(GetSdlWindow());
        return (flags && SDL_WINDOW_FULLSCREEN) == 1;
    }

    vk::SurfaceKHR SDLWindow::CreateSurface(const vk::Instance& instance)
    {
        VkSurfaceKHR surface;
        SDL_Vulkan_CreateSurface(GetSdlWindow(),static_cast<VkInstance>(instance),nullptr,&surface);
        return surface;
    }

    std::vector<std::string> SDLWindow::GetRequiredExtensions()
    {
        std::vector<std::string> result{};
        Uint32 numExt{};
        const auto extensions = SDL_Vulkan_GetInstanceExtensions(&numExt);
        for(auto i = 0; i < numExt; i++)
        {
            result.push_back(std::string(extensions[i]));
        }
        
        return result;
    }

    void SDLWindow::HandleEvent(const SDL_Event& event)
    {
        
    }

    SDL_Window* SDLWindow::GetSdlWindow() const
    {
        return _ptr;
    }

    void SDLWindow::SetParent(SDLWindow* parent)
    {
        SDL_SetWindowParent(GetSdlWindow(),parent->GetSdlWindow());
        _parent = parent;
    }
    
}
