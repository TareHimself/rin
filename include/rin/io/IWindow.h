#pragma once
#include "rin/core/IDisposable.h"
#include "rin/core/memory.h"
#include "rin/core/math/Vec2.h"
#include <string>
#include <optional>
#include <vulkan/vulkan.hpp>
#include "rin/core/delegates/DelegateList.h"

namespace rin::io
{
    class IWindow : public IDisposable
    {
    public:
        struct CreateOptions
        {
           
            // Whether the windowed mode window will be resizable by the user
            bool resizable = true;
            // Whether the windowed mode window will be initially visible
            bool visible = true;
            /*
            * Whether the windowed mode window will have window decorations such as a border, a close widget, etc.
            * An undecorated window will not be resizable by the user but will still allow the user to generate close events on
            * some platforms.
            */
            bool decorated = true;
            // Specifies whether the windowed mode window will be given input focus when created.
            bool focused = true;
            /*
             * Whether the windowed mode window will be floating above other regular windows, also called topmost or
             * always-on-top.
             * This is intended primarily for debugging purposes and cannot be used to implement proper full screen windows.
             */
            bool floating = false;
            // Whether the windowed mode window will be maximized when created.
            bool maximized = false;
            bool transparent = false;


            CreateOptions& Resizable(bool state);
            CreateOptions& Visible(bool state);
            CreateOptions& Decorated(bool state);
            CreateOptions& Focused(bool state);
            CreateOptions& Floating(bool state);
            CreateOptions& Maximized(bool state);
            CreateOptions& Transparent(bool state);
        };
        
        struct Event
        {
            Shared<IWindow> window{};
            Event(const Shared<IWindow>& inWindow)
            {
                window = inWindow;
            }
        };

        struct KeyEvent : Event
        {
            KeyEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        struct CursorMoveEvent : Event
        {
            CursorMoveEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        struct ButtonEvent : Event
        {
            ButtonEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        struct FocusEvent : Event
        {
            FocusEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        struct ScrollEvent : Event
        {
            ScrollEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };
        
        struct ResizeEvent : Event
        {
            ResizeEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };
        
        struct CharacterEvent : Event
        {
            CharacterEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        struct MaximizeEvent : Event
        {
            MaximizeEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        struct MinimizeEvent : Event
        {
            MinimizeEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        struct RefreshEvent : Event
        {
            RefreshEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        struct DropEvent : Event
        {
            DropEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        struct CloseEvent : Event
        {
            CloseEvent(const Shared<IWindow>& inWindow) : Event(inWindow)
            {
            
            }
        };

        virtual Vec2<double> GetCursorPosition() = 0;
        virtual void SetCursorPosition(const Vec2<double>& position) = 0;
        virtual void SetFullscreen(bool state) = 0;
        virtual Vec2<uint32_t> GetPixelSize() = 0;
        virtual IWindow * CreateChild(const Vec2<int>& size,const std::string& name,const std::optional<CreateOptions>& options = {}) = 0;
        virtual IWindow * GetParent() = 0;
        virtual bool IsFocused() = 0;
        virtual bool IsFullscreen() = 0;

        virtual vk::SurfaceKHR CreateSurface(const vk::Instance& instance) = 0;
        virtual std::vector<std::string> GetRequiredExtensions() = 0;
        
        DEFINE_DELEGATE_LIST(onDisposed)
        DEFINE_DELEGATE_LIST(onKey,const Shared<KeyEvent>&)
        DEFINE_DELEGATE_LIST(onButton,const Shared<ButtonEvent>&)
        DEFINE_DELEGATE_LIST(onCursorMoved,const Shared<CursorMoveEvent>&)
        DEFINE_DELEGATE_LIST(onFocus,const Shared<FocusEvent>&)
        DEFINE_DELEGATE_LIST(onScroll,const Shared<ScrollEvent>&)
        DEFINE_DELEGATE_LIST(onResize,const Shared<ResizeEvent>&)
        DEFINE_DELEGATE_LIST(onClose,const Shared<CloseEvent>&)
        DEFINE_DELEGATE_LIST(onCharacter,const Shared<CharacterEvent>&)
        DEFINE_DELEGATE_LIST(onRefresh,const Shared<RefreshEvent>&)
        DEFINE_DELEGATE_LIST(onMaximize,const Shared<MaximizeEvent>&)
        DEFINE_DELEGATE_LIST(onMinimize,const Shared<MinimizeEvent>&)
        DEFINE_DELEGATE_LIST(onDrop,const Shared<DropEvent>&)
    };
}
