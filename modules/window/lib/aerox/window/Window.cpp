#include "aerox/window/Window.hpp"
#include <GLFW/glfw3.h>
#include "aerox/window/WindowModule.hpp"
#include "aerox/core/GRuntime.hpp"

namespace aerox::window
{
    Window::Window(GLFWwindow* inWindow)
    {
        _window = inWindow;
        if (_window != nullptr)
        {
            glfwSetWindowUserPointer(_window, this);
            glfwSetKeyCallback(_window, [](GLFWwindow* window, int key, int scancode, int action, int mods)
            {
                const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
                self->onKey->Invoke(self,static_cast<EKey>(key), static_cast<EInputState>(action));
            });
            glfwSetCursorPosCallback(_window, [](GLFWwindow* window, double x, double y)
            {
                const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
                self->onCursorMoved->Invoke(self,Vec2<float>{static_cast<float>(x), static_cast<float>(y)});
            });
            glfwSetMouseButtonCallback(_window, [](GLFWwindow* window, int button, int action, int mods)
            {
                const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
                self->onCursorButton->Invoke(self,static_cast<ECursorButton>(button), static_cast<EInputState>(action));
            });

            //
            // glfwSetScrollCallback(_window, [](GLFWwindow* window, const double xoffset, const double yoffset)
            // {
            //     const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
            //     self->HandleScroll(xoffset, yoffset);
            // });
            //
            glfwSetWindowFocusCallback(_window, [](GLFWwindow* window, int focused)
            {
                const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
                const auto newFocus = static_cast<bool>(focused);
                self->onFocusChanged->Invoke(self, newFocus);
            });

            glfwSetWindowSizeCallback(_window, [](GLFWwindow* window, const int width, const int height)
            {
                const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
                self->onResize->Invoke(self,Vec2{width,height});
            });

            glfwSetFramebufferSizeCallback(_window, [](GLFWwindow* window, const int width, const int height)
            {
                const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
                self->onFrameBufferResize->Invoke(self,Vec2{width,height});
            });

            glfwSetWindowCloseCallback(_window, [](GLFWwindow* window)
            {
                const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
                self->onCloseRequested->Invoke(self);
            });

            if (const auto windowModule = GetWindowModule())
            {
                windowModule->onWindowCreated->Invoke(this);
            }
        }
    }

    Window::~Window()
    {
        if (const auto windowModule = GetWindowModule())
        {
            windowModule->onWindowDestroyed->Invoke(this);
        }

        glfwDestroyWindow(_window);
        _window = nullptr;
    }

    GLFWwindow* Window::GetGlfwWindow() const
    {
        return _window;
    }

    Vec2<int> Window::GetSize() const
    {
        Vec2 result{0};
        glfwGetWindowSize(_window,&result.x,&result.y);
        return result;
    }

    Vec2<int> Window::GetFrameBufferSize() const
    {
        Vec2 result{0};
        glfwGetFramebufferSize(_window,&result.x,&result.y);
        return result;
    }

    Vec2<float> Window::GetCursorPosition() const
    {
        Vec2<double> position{0.0};
        glfwGetCursorPos(_window,&position.x,&position.y);
        return position.Cast<float>();
    }

    WindowModule* Window::GetWindowModule()
    {
        return GRuntime::Get()->GetModule<WindowModule>();
    }
}