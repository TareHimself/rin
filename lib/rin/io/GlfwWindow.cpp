#include "rin/io/GlfwWindow.h"
#include <GLFW/glfw3.h>

#include "rin/io/InputKey.h"
#include "rin/io/IoModule.h"
#include "rin/io/events/CharacterEvent.h"
#include "rin/io/events/CloseEvent.h"
#include "rin/io/events/CursorButtonEvent.h"
#include "rin/io/events/CursorMoveEvent.h"
#include "rin/io/events/DropEvent.h"
#include "rin/io/events/FocusEvent.h"
#include "rin/io/events/KeyEvent.h"
#include "rin/io/events/MaximizeEvent.h"
#include "rin/io/events/MinimizeEvent.h"
#include "rin/io/events/ResizeEvent.h"
#include "rin/io/events/ScrollEvent.h"
namespace rin::io
{
    InputKey fromGlfwKey(const int& key)
    {
        switch(key)
        {
        // ASCII printable keys
        case GLFW_KEY_SPACE: return InputKey::Space;
        case GLFW_KEY_APOSTROPHE: return InputKey::Apostrophe;
        case GLFW_KEY_COMMA: return InputKey::Comma;
        case GLFW_KEY_MINUS: return InputKey::Minus;
        case GLFW_KEY_PERIOD: return InputKey::Period;
        case GLFW_KEY_SLASH: return InputKey::Slash;
        case GLFW_KEY_0: return InputKey::Zero;
        case GLFW_KEY_1: return InputKey::One;
        case GLFW_KEY_2: return InputKey::Two;
        case GLFW_KEY_3: return InputKey::Three;
        case GLFW_KEY_4: return InputKey::Four;
        case GLFW_KEY_5: return InputKey::Five;
        case GLFW_KEY_6: return InputKey::Six;
        case GLFW_KEY_7: return InputKey::Seven;
        case GLFW_KEY_8: return InputKey::Eight;
        case GLFW_KEY_9: return InputKey::Nine;
        case GLFW_KEY_SEMICOLON: return InputKey::Semicolon;
        case GLFW_KEY_EQUAL: return InputKey::Equal;
        case GLFW_KEY_A: return InputKey::A;
        case GLFW_KEY_B: return InputKey::B;
        case GLFW_KEY_C: return InputKey::C;
        case GLFW_KEY_D: return InputKey::D;
        case GLFW_KEY_E: return InputKey::E;
        case GLFW_KEY_F: return InputKey::F;
        case GLFW_KEY_G: return InputKey::G;
        case GLFW_KEY_H: return InputKey::H;
        case GLFW_KEY_I: return InputKey::I;
        case GLFW_KEY_J: return InputKey::J;
        case GLFW_KEY_K: return InputKey::K;
        case GLFW_KEY_L: return InputKey::L;
        case GLFW_KEY_M: return InputKey::M;
        case GLFW_KEY_N: return InputKey::N;
        case GLFW_KEY_O: return InputKey::O;
        case GLFW_KEY_P: return InputKey::P;
        case GLFW_KEY_Q: return InputKey::Q;
        case GLFW_KEY_R: return InputKey::R;
        case GLFW_KEY_S: return InputKey::S;
        case GLFW_KEY_T: return InputKey::T;
        case GLFW_KEY_U: return InputKey::U;
        case GLFW_KEY_V: return InputKey::V;
        case GLFW_KEY_W: return InputKey::W;
        case GLFW_KEY_X: return InputKey::X;
        case GLFW_KEY_Y: return InputKey::Y;
        case GLFW_KEY_Z: return InputKey::Z;
        case GLFW_KEY_LEFT_BRACKET: return InputKey::LeftBracket;
        case GLFW_KEY_BACKSLASH: return InputKey::Backslash;
        case GLFW_KEY_RIGHT_BRACKET: return InputKey::RightBracket;
        case GLFW_KEY_GRAVE_ACCENT: return InputKey::GraveAccent;
        case GLFW_KEY_WORLD_1: return InputKey::World1;
        case GLFW_KEY_WORLD_2: return InputKey::World2;

        // Function keys
        case GLFW_KEY_ESCAPE: return InputKey::Escape;
        case GLFW_KEY_ENTER: return InputKey::Enter;
        case GLFW_KEY_TAB: return InputKey::Tab;
        case GLFW_KEY_BACKSPACE: return InputKey::Backspace;
        case GLFW_KEY_INSERT: return InputKey::Insert;
        case GLFW_KEY_DELETE: return InputKey::Delete;
        case GLFW_KEY_RIGHT: return InputKey::Right;
        case GLFW_KEY_LEFT: return InputKey::Left;
        case GLFW_KEY_DOWN: return InputKey::Down;
        case GLFW_KEY_UP: return InputKey::Up;
        case GLFW_KEY_PAGE_UP: return InputKey::PageUp;
        case GLFW_KEY_PAGE_DOWN: return InputKey::PageDown;
        case GLFW_KEY_HOME: return InputKey::Home;
        case GLFW_KEY_END: return InputKey::End;
        case GLFW_KEY_CAPS_LOCK: return InputKey::CapsLock;
        case GLFW_KEY_SCROLL_LOCK: return InputKey::ScrollLock;
        case GLFW_KEY_NUM_LOCK: return InputKey::NumLock;
        case GLFW_KEY_PRINT_SCREEN: return InputKey::PrintScreen;
        case GLFW_KEY_PAUSE: return InputKey::Pause;
        case GLFW_KEY_F1: return InputKey::F1;
        case GLFW_KEY_F2: return InputKey::F2;
        case GLFW_KEY_F3: return InputKey::F3;
        case GLFW_KEY_F4: return InputKey::F4;
        case GLFW_KEY_F5: return InputKey::F5;
        case GLFW_KEY_F6: return InputKey::F6;
        case GLFW_KEY_F7: return InputKey::F7;
        case GLFW_KEY_F8: return InputKey::F8;
        case GLFW_KEY_F9: return InputKey::F9;
        case GLFW_KEY_F10: return InputKey::F10;
        case GLFW_KEY_F11: return InputKey::F11;
        case GLFW_KEY_F12: return InputKey::F12;
        case GLFW_KEY_F13: return InputKey::F13;
        case GLFW_KEY_F14: return InputKey::F14;
        case GLFW_KEY_F15: return InputKey::F15;
        case GLFW_KEY_F16: return InputKey::F16;
        case GLFW_KEY_F17: return InputKey::F17;
        case GLFW_KEY_F18: return InputKey::F18;
        case GLFW_KEY_F19: return InputKey::F19;
        case GLFW_KEY_F20: return InputKey::F20;
        case GLFW_KEY_F21: return InputKey::F21;
        case GLFW_KEY_F22: return InputKey::F22;
        case GLFW_KEY_F23: return InputKey::F23;
        case GLFW_KEY_F24: return InputKey::F24;
        case GLFW_KEY_F25: return InputKey::F25;
        case GLFW_KEY_KP_0: return InputKey::KP0;
        case GLFW_KEY_KP_1: return InputKey::KP1;
        case GLFW_KEY_KP_2: return InputKey::KP2;
        case GLFW_KEY_KP_3: return InputKey::KP3;
        case GLFW_KEY_KP_4: return InputKey::KP4;
        case GLFW_KEY_KP_5: return InputKey::KP5;
        case GLFW_KEY_KP_6: return InputKey::KP6;
        case GLFW_KEY_KP_7: return InputKey::KP7;
        case GLFW_KEY_KP_8: return InputKey::KP8;
        case GLFW_KEY_KP_9: return InputKey::KP9;
        case GLFW_KEY_KP_DECIMAL: return InputKey::KPDecimal;
        case GLFW_KEY_KP_DIVIDE: return InputKey::KPDivide;
        case GLFW_KEY_KP_MULTIPLY: return InputKey::KPMultiply;
        case GLFW_KEY_KP_SUBTRACT: return InputKey::KPSubtract;
        case GLFW_KEY_KP_ADD: return InputKey::KPAdd;
        case GLFW_KEY_KP_ENTER: return InputKey::KPEnter;
        case GLFW_KEY_KP_EQUAL: return InputKey::KPEqual;
        case GLFW_KEY_LEFT_SHIFT: return InputKey::LeftShift;
        case GLFW_KEY_LEFT_CONTROL: return InputKey::LeftControl;
        case GLFW_KEY_LEFT_ALT: return InputKey::LeftAlt;
        case GLFW_KEY_LEFT_SUPER: return InputKey::LeftSuper;
        case GLFW_KEY_RIGHT_SHIFT: return InputKey::RightShift;
        case GLFW_KEY_RIGHT_CONTROL: return InputKey::RightControl;
        case GLFW_KEY_RIGHT_ALT: return InputKey::RightAlt;
        case GLFW_KEY_RIGHT_SUPER: return InputKey::RightSuper;
        case GLFW_KEY_MENU: return InputKey::Menu;

        // Default case for unmapped keys
        default: throw std::invalid_argument("Unsupported GLFW key");
        }
    }

    CursorButton fromGlfwButton(const int& button)
    {
        return static_cast<CursorButton>(button);
    }

    InputState fromGlfwAction(const int& action)
    {
        auto rinAction = InputState::Pressed;
        if(action == GLFW_PRESS)
        {
            rinAction = InputState::Pressed;
        }
        else if(action == GLFW_RELEASE)
        {
            rinAction = InputState::Released;
        }
        else if(action == GLFW_REPEAT)
        {
            rinAction = InputState::Repeat;
        }
        return rinAction;
    }
    Flags<InputModifier> fromGlfwModifiers(const int& mods)
    {
        Flags<InputModifier> modifier{};
        if(mods & GLFW_MOD_SHIFT)
        {
            modifier.Add(InputModifier::Shift);
        }
        if(mods & GLFW_MOD_CONTROL)
        {
            modifier.Add(InputModifier::Control);
        }
        if(mods & GLFW_MOD_ALT)
        {
            modifier.Add(InputModifier::Alt);
        }
        if(mods & GLFW_MOD_SUPER)
        {
            modifier.Add(InputModifier::Super);
        }
        if(mods & GLFW_MOD_CAPS_LOCK)
        {
            modifier.Add(InputModifier::CapsLock);
        }
        if(mods & GLFW_MOD_NUM_LOCK)
        {
            modifier.Add(InputModifier::NumLock);
        }
        return modifier;
    }
    GlfwWindow::GlfwWindow(GLFWwindow* ptr)
    {
        _ptr = ptr;
        glfwSetWindowUserPointer(GetGlfwWindow(),this);
        glfwSetWindowCloseCallback(GetGlfwWindow(),[](GLFWwindow* ptr){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onClose->Invoke(shared<CloseEvent>(self));
        });
        glfwSetWindowSizeCallback(GetGlfwWindow(),[](GLFWwindow* ptr, int width, int height){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onResize->Invoke(shared<ResizeEvent>(self,Vec2{width, height}));
        });
        glfwSetFramebufferSizeCallback(GetGlfwWindow(),[](GLFWwindow* ptr, int width, int height){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onFrameBufferResize->Invoke(shared<ResizeEvent>(self,Vec2{width, height}));
        });
        glfwSetWindowMaximizeCallback(GetGlfwWindow(),[](GLFWwindow* ptr, int maximized){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onMaximize->Invoke(shared<MaximizeEvent>(self,maximized == 1));
        });
        glfwSetWindowFocusCallback(GetGlfwWindow(),[](GLFWwindow* ptr, int focused){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->_focused = focused == 1;
            self->onFocus->Invoke(shared<FocusEvent>(self,self->_focused));
        });

        glfwSetScrollCallback(GetGlfwWindow(),[](GLFWwindow* ptr, const double xoffset, const double yoffset){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onScroll->Invoke(shared<ScrollEvent>(self,static_cast<Vec2<>>(Vec2{xoffset, yoffset})));
        });

        glfwSetCharModsCallback(GetGlfwWindow(),[](GLFWwindow* ptr, unsigned int codepoint, int mods){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            auto character = static_cast<char>(codepoint);
            self->onCharacter->Invoke(shared<CharacterEvent>(self,character,fromGlfwModifiers(mods)));
        });

        glfwSetCursorPosCallback(GetGlfwWindow(),[](GLFWwindow* ptr, double xpos, double ypos){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onCursorMove->Invoke(shared<CursorMoveEvent>(self,static_cast<Vec2<>>(Vec2{xpos, ypos})));
        });

        glfwSetMouseButtonCallback(GetGlfwWindow(),[](GLFWwindow* ptr, int button, int action, int mods){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onCursorButton->Invoke(shared<CursorButtonEvent>(self,fromGlfwButton(button),fromGlfwAction(action),self->GetCursorPosition(),fromGlfwModifiers(mods)));
        });

        glfwSetKeyCallback(GetGlfwWindow(),[](GLFWwindow* ptr, int key, int scancode, int action, int mods){
            auto self = static_cast<GlfwWindow*>(glfwGetWindowUserPointer(ptr));
            self->onKey->Invoke(shared<KeyEvent>(self,fromGlfwKey(key),fromGlfwAction(action),fromGlfwModifiers(mods)));
        });
    }

    void GlfwWindow::OnDispose()
    {
        for(const auto child : _children)
        {
            child->Dispose();
        }

        _children.clear();

        onDisposed->Invoke();
    }

    Vec2<> GlfwWindow::GetCursorPosition()
    {
        Vec2<double> position{0};
        //
        // SDL_GetWindowPosition(GetSdlWindow(),&position.x,&position.y);
        // return static_cast<Vec2<double>>(position);
        glfwGetCursorPos(GetGlfwWindow(),&position.x,&position.y);
        return static_cast<Vec2<>>(position);
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
            int midpointX, midpointY;
            {
                int x, y, width, height;
                glfwGetWindowPos(window,&x,&y);
                glfwGetWindowSize(window,&width,&height);
                midpointX = x + (width / 2);
                midpointY = y + (height / 2);
            }
            for(auto i = 0; i < numMonitors; i++)
            {
                auto monitor = monitors[i];
                int x, y, width, height;
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
                int x, y, width, height;
                glfwGetMonitorWorkarea(monitor,&x,&y,&width,&height);
                width /= 2;
                height /= 2;
                x += width / 2;
                y += height / 2;
                glfwSetWindowMonitor(GetGlfwWindow(),nullptr,x,y,width,height,GLFW_DONT_CARE);
            }
        }
    }

    Vec2<uint32_t> GlfwWindow::GetSize()
    {
        Vec2<int> size{0};
        glfwGetWindowSize(GetGlfwWindow(),&size.x,&size.y);
        return static_cast<Vec2<uint32_t>>(size);
    }
    Vec2<uint32_t> GlfwWindow::GetFrameBufferSize()
    {
        Vec2<int> size{0};
        glfwGetFramebufferSize(GetGlfwWindow(),&size.x,&size.y);
        return static_cast<Vec2<uint32_t>>(size);
    }

    Window* GlfwWindow::CreateChild(const Vec2<int>& size, const std::string& name,
        const std::optional<CreateOptions>& options)
    {
        const auto child = IoModule::Get()->CreateWindow(size,name,options,this);
        _children.push_back(child);
        return child;
    }

    Window* GlfwWindow::GetParent()
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
        glfwCreateWindowSurface(instance,GetGlfwWindow(),nullptr,&surface);
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
