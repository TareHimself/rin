#pragma once
#include "IWindow.h"
struct GLFWwindow;
namespace rin::io
{
    class GlfwWindow : public IWindow
    {
        GLFWwindow* _ptr;
        GlfwWindow * _parent{};
        std::vector<IWindow *> _children{};
        bool _fullscreen{false};
        bool _focused{false};
    protected:
        void OnDispose() override;
        
    public:
        GlfwWindow(GLFWwindow * ptr);
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
        GLFWwindow * GetGlfwWindow() const;
        void SetParent(GlfwWindow * parent);
    };
}
