#include "aerox/window/WindowCreateOptions.hpp"
#include <GLFW/glfw3.h>
namespace aerox::window
{
    void WindowCreateOptions::Apply() const
    {
        glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
        glfwWindowHint(GLFW_RESIZABLE, _resizable ? GLFW_TRUE : GLFW_FALSE);
        glfwWindowHint(GLFW_VISIBLE, _visible ? GLFW_TRUE : GLFW_FALSE);
        glfwWindowHint(GLFW_DECORATED, _decorated ? GLFW_TRUE : GLFW_FALSE);
        glfwWindowHint(GLFW_FLOATING, _floating ? GLFW_TRUE : GLFW_FALSE);
        glfwWindowHint(GLFW_MAXIMIZED, _maximized ? GLFW_TRUE : GLFW_FALSE);
        glfwWindowHint(GLFW_CENTER_CURSOR, _cursorCentered ? GLFW_TRUE : GLFW_FALSE);
    }

    WindowCreateOptions& WindowCreateOptions::Resizable(bool newState)
    {
        _resizable = newState;
        return *this;
    }

    WindowCreateOptions& WindowCreateOptions::Visible(bool newState)
    {
        _visible = newState;
        return *this;
    }

    WindowCreateOptions& WindowCreateOptions::Decorated(bool newState)
    {
        _decorated = newState;
        return *this;
    }

    WindowCreateOptions& WindowCreateOptions::Focused(bool newState)
    {
        _focused = newState;
        return *this;
    }

    WindowCreateOptions& WindowCreateOptions::Floating(bool newState)
    {
        _floating = newState;
        return *this;
    }

    WindowCreateOptions& WindowCreateOptions::Maximized(bool newState)
    {
        _maximized = newState;
        return *this;
    }

    WindowCreateOptions& WindowCreateOptions::CursorCentered(bool newState)
    {
        _cursorCentered = newState;
        return *this;
    }
}
