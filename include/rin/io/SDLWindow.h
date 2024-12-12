#pragma once
#include "IWindow.h"
struct SDL_Window;
union SDL_Event;
namespace rin::io
{
    class SDLWindow : public IWindow
    {
        SDL_Window * _ptr;
        SDLWindow * _parent{};
        std::vector<IWindow *> _children{};
    protected:
        void OnDispose() override;
        
    public:
        SDLWindow(SDL_Window * ptr);
        Vec2<double> GetCursorPosition() override;
        void SetCursorPosition(const Vec2<double>& position) override;
        void SetFullscreen(bool state) override;
        Vec2<uint32_t> GetPixelSize() override;
        IWindow * CreateChild(const Vec2<int>& size, const std::string& name,
            const std::optional<CreateOptions>& options) override;
        IWindow * GetParent() override;
        bool IsFocused() override;
        bool IsFullscreen() override;
        vk::SurfaceKHR CreateSurface(const vk::Instance& instance) override;
        std::vector<std::string> GetRequiredExtensions() override;
        void HandleEvent(const SDL_Event& event);
        SDL_Window * GetSdlWindow() const;
        void SetParent(SDLWindow * parent);
    };
}
