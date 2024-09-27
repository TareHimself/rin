#pragma once
#include <SDL3/SDL_events.h>

#include "CursorButton.hpp"
#include "InputState.hpp"
#include "Key.hpp"
#include "rin/core/delegates/DelegateList.hpp"
#include "rin/core/math/Vec2.hpp"
#include "rin/core/RinBase.hpp"

struct SDL_Window;
class WindowModule;
    

class Window : public RinBase
{
        
    SDL_Window* _window = nullptr;


protected:
    void NotifyEvent(const SDL_Event& event);
public:
    friend WindowModule;
    explicit Window(SDL_Window* inWindow);

    ~Window() override;
    //   DECLARE_DELEGATE(onKeyUp,const std::shared_ptr<KeyEvent> &)
    // DECLARE_DELEGATE(onKeyDown,const std::shared_ptr<KeyEvent> &)
    // DECLARE_DELEGATE(onMouseDown,const std::shared_ptr<MouseButtonEvent> &)
    // DECLARE_DELEGATE(onMouseMoved,const std::shared_ptr<MouseMovedEvent> &)
    // DECLARE_DELEGATE(onMouseUp,const std::shared_ptr<MouseButtonEvent> &)
    // DECLARE_DELEGATE(onScroll,const std::shared_ptr<ScrollEvent> &)
    // DECLARE_DELEGATE(onFocusChanged,const std::weak_ptr<Window> &, bool)
    // DECLARE_DELEGATE(onResize,const std::weak_ptr<Window> &)
    // DECLARE_DELEGATE(onCloseRequested,const std::weak_ptr<Window> &)

    DEFINE_DELEGATE_LIST(onFocusChanged,Window*,bool)
    DEFINE_DELEGATE_LIST(onResize,Window*,const Vec2<int>&)
    DEFINE_DELEGATE_LIST(onCloseRequested,Window*)
    DEFINE_DELEGATE_LIST(onKey,Window*,Key,InputState)
    DEFINE_DELEGATE_LIST(onCursorButton,Window*,CursorButton,InputState)
    DEFINE_DELEGATE_LIST(onCursorMoved,Window*,const Vec2<float>&)
    DEFINE_DELEGATE_LIST(onDisposed,Window*)

    SDL_Window* GetSDLWindow() const;

    Vec2<int> GetSize() const;
    Vec2<int> GetPosition() const;
    Vec2<float> GetCursorPosition() const;
    void OnDispose(bool manual) override;
protected:
    WindowModule * GetWindowModule();
};
