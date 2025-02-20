#pragma once
#include "Window.h"
struct GLFWwindow;
namespace rin::io
{
    class GlfwWindow : public Window
    {
        GLFWwindow* _ptr;
        GlfwWindow * _parent{};
        std::vector<Window *> _children{};
        bool _fullscreen{false};
        bool _focused{false};
    protected:
        void OnDispose() override;
        
    public:
        GlfwWindow(GLFWwindow * ptr);
        Vec2<> GetCursorPosition() override;
        void SetCursorPosition(const Vec2<double>& position) override;
        void SetFullscreen(bool state) override;
        Vec2<uint32_t> GetSize() override;
        Vec2<uint32_t> GetFrameBufferSize() override;
        Window * CreateChild(const Vec2<int>& size, const std::string& name,
            const std::optional<CreateOptions>& options) override;
        Window * GetParent() override;
        bool IsFocused() override;
        bool IsFullscreen() override;
        vk::SurfaceKHR CreateSurface(const vk::Instance& instance) override;
        std::vector<std::string> GetRequiredExtensions() override;
        GLFWwindow * GetGlfwWindow() const;
        void SetParent(GlfwWindow * parent);
    };
}
