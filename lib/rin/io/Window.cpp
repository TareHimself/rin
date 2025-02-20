#include "rin/io/Window.h"

namespace rin::io
{
    Window::CreateOptions& Window::CreateOptions::Resizable(bool state)
    {
        resizable = state;
        return *this;
    }

    Window::CreateOptions& Window::CreateOptions::Visible(bool state)
    {
        visible = state;
        return *this;
    }

    Window::CreateOptions& Window::CreateOptions::Decorated(bool state)
    {
        decorated = state;
        return *this;
    }

    Window::CreateOptions& Window::CreateOptions::Focused(bool state)
    {
        focused = state;
        return *this;
    }

    Window::CreateOptions& Window::CreateOptions::Floating(bool state)
    {
        floating = state;
        return *this;
    }

    Window::CreateOptions& Window::CreateOptions::Maximized(bool state)
    {
        maximized = state;
        return *this;
    }
    

    Window::CreateOptions& Window::CreateOptions::Transparent(bool state)
    {
        transparent = state;
        return *this;
    }

    
}
