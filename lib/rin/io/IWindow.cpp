#include "rin/io/IWindow.h"

namespace rin::io
{
    IWindow::CreateOptions& IWindow::CreateOptions::Resizable(bool state)
    {
        resizable = state;
        return *this;
    }

    IWindow::CreateOptions& IWindow::CreateOptions::Visible(bool state)
    {
        visible = state;
        return *this;
    }

    IWindow::CreateOptions& IWindow::CreateOptions::Decorated(bool state)
    {
        decorated = state;
        return *this;
    }

    IWindow::CreateOptions& IWindow::CreateOptions::Focused(bool state)
    {
        focused = state;
        return *this;
    }

    IWindow::CreateOptions& IWindow::CreateOptions::Floating(bool state)
    {
        floating = state;
        return *this;
    }

    IWindow::CreateOptions& IWindow::CreateOptions::Maximized(bool state)
    {
        maximized = state;
        return *this;
    }
    

    IWindow::CreateOptions& IWindow::CreateOptions::Transparent(bool state)
    {
        transparent = state;
        return *this;
    }

    
}
