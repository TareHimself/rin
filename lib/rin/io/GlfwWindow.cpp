#include "rin/io/GlfwWindow.h"
#include <GLFW/glfw3.h>
#include "rin/io/IoModule.h"
namespace rin::io
{
    GlfwWindow::GlfwWindow(GLFWwindow* ptr)
    {
        _ptr = ptr;
        glfwSetWindowUserPointer(GetGlfwWindow(),this);
        glfwSetWindowCloseCallback(GetGlfwWindow(),[](GLFWwindow* ptr)
        {
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onClose->Invoke(shared<CloseEvent>(self));
        });
        glfwSetFramebufferSizeCallback(GetGlfwWindow(),[](GLFWwindow* ptr,int width,int height)
        {
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onResize->Invoke(shared<ResizeEvent>(self,Vec2{width,height}));
        });
        glfwSetWindowMaximizeCallback(GetGlfwWindow(),[](GLFWwindow* ptr, int maximized)
        {
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onMaximize->Invoke(shared<MaximizeEvent>(self,maximized == 1));
        });
        glfwSetWindowFocusCallback(GetGlfwWindow(),[](GLFWwindow* ptr, int focused)
        {
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->_focused = focused == 1;
            self->onFocus->Invoke(shared<FocusEvent>(self,self->_focused));
        });

        glfwSetScrollCallback(GetGlfwWindow(),[](GLFWwindow* ptr, const double xoffset, const double yoffset)
        {
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onScroll->Invoke(shared<ScrollEvent>(self,Vec2{xoffset,yoffset}));
        });

        glfwSetCharModsCallback(GetGlfwWindow(),[](GLFWwindow* ptr, unsigned int codepoint, int mods)
        {
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onCharacter->Invoke(shared<CharacterEvent>(self,static_cast<char>(codepoint)));
        });
    }
    
    void GlfwWindow::OnDispose()
    {
        for (const auto child : _children)
        {
            child->Dispose();
        }

        _children.clear();
        
        onDisposed->Invoke();
    }

    Vec2<double> GlfwWindow::GetCursorPosition()
    {
        Vec2<double> position{0};
        //
        // SDL_GetWindowPosition(GetSdlWindow(),&position.x,&position.y);
        // return static_cast<Vec2<double>>(position);
        glfwGetCursorPos(GetGlfwWindow(),&position.x,&position.y);
        return position;
    }

    void GlfwWindow::SetCursorPosition(const Vec2<double>& position)
    {
        glfwSetCursorPos(GetGlfwWindow(),position.x,position.y);
    }

    void GlfwWindow::SetFullscreen(bool state)
    {
        if(state)
        {
            if(IsFullscreen())
            {
                return;
            }

            auto window = GetGlfwWindow();
            int numMonitors = 0;
            auto monitors = glfwGetMonitors(&numMonitors);
            int midpointX,midpointY;
            {
                int x,y,width,height;
                glfwGetWindowPos(window,&x,&y);
                glfwGetWindowSize(window,&width,&height);
                midpointX = x + (width / 2);
                midpointY = y + (height / 2);
            }
            for(auto i = 0; i < numMonitors; i++)
            {
                auto monitor = monitors[i];
                int x,y,width,height;
                glfwGetMonitorWorkarea(monitor,&x,&y,&width,&height);
                if(x <= midpointX && y <= midpointY && midpointX <= (x + width) && midpointY <= (y + height))
                {
                    glfwSetWindowMonitor(window,monitor,0,0,width,height,GLFW_DONT_CARE);
                    return;
                }
            }
        }
        else
        {
            if(auto monitor = glfwGetWindowMonitor(GetGlfwWindow()))
            {
                int x,y,width,height;
                glfwGetMonitorWorkarea(monitor,&x,&y,&width,&height);
                width /= 2;
                height /= 2;
                x += width / 2;
                y += height / 2;
                glfwSetWindowMonitor(GetGlfwWindow(),nullptr,x,y,width,height,GLFW_DONT_CARE);
            }
        }
    }

    Vec2<uint32_t> GlfwWindow::GetPixelSize()
    {
        Vec2<int> size{0};
        glfwGetFramebufferSize(GetGlfwWindow(),&size.x,&size.y);
        return static_cast<Vec2<uint32_t>>(size);
    }

    IWindow * GlfwWindow::CreateChild(const Vec2<int>& size, const std::string& name,
        const std::optional<CreateOptions>& options)
    {
        const auto child = IoModule::Get()->CreateWindow(size,name,options,this);
        _children.push_back(child);
        return child;
    }

    IWindow * GlfwWindow::GetParent()
    {
        return _parent;
    }

    bool GlfwWindow::IsFocused()
    {
        return _focused;
    }

    bool GlfwWindow::IsFullscreen()
    {
        return glfwGetWindowMonitor(GetGlfwWindow()) != nullptr;
    }

    vk::SurfaceKHR GlfwWindow::CreateSurface(const vk::Instance& instance)
    {
        VkSurfaceKHR surface;
        glfwCreateWindowSurface(instance,GetGlfwWindow(), nullptr,&surface);
        return surface;
    }

    std::vector<std::string> GlfwWindow::GetRequiredExtensions()
    {
        std::vector<std::string> result{};
        uint32_t numExt{};
        const auto extensions = glfwGetRequiredInstanceExtensions(&numExt);
        for(auto i = 0; i < numExt; i++)
        {
            result.emplace_back(extensions[i]);
        }
        
        return result;
    }

    GLFWwindow* GlfwWindow::GetGlfwWindow() const
    {
        return _ptr;
    }

    void GlfwWindow::SetParent(GlfwWindow* parent)
    {
        _parent = parent;
    }
    
}
