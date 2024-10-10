#include "rin/window/Window.hpp"
#include "rin/window/WindowModule.hpp"
#include "rin/core/GRuntime.hpp"

void Window::NotifyEvent(const SDL_Event& event)
    {
        switch (event.type)
        {
        case SDL_EVENT_KEY_UP:
        case SDL_EVENT_KEY_DOWN:
            {
                if (event.key.repeat)
                {
                    onKey->Invoke(this, static_cast<Key>(event.key.key), InputState::Repeat);
                }
                else
                {
                    onKey->Invoke(this, static_cast<Key>(event.key.key),
                                  event.type == SDL_EVENT_KEY_UP ? InputState::Released : InputState::Pressed);
                }
            }
            break;
        case SDL_EVENT_MOUSE_MOTION:
            {
                onCursorMoved->Invoke(this, GetPosition().Cast<float>() + Vec2<float>{
                                          static_cast<float>(event.motion.x), static_cast<float>(event.motion.y)
                                      });
            }
            break;
        case SDL_EVENT_MOUSE_BUTTON_UP:
        case SDL_EVENT_MOUSE_BUTTON_DOWN:
            {
                onCursorButton->Invoke(this, static_cast<CursorButton>(event.button.button),
                                       event.button.down
                                           ? InputState::Pressed
                                           : InputState::Released);
            }
            break;
        case SDL_EVENT_WINDOW_FOCUS_GAINED:
        case SDL_EVENT_WINDOW_FOCUS_LOST:
            onFocusChanged->Invoke(this, event.type == SDL_EVENT_WINDOW_FOCUS_GAINED);
            break;
        case SDL_EVENT_WINDOW_RESIZED:
            {
                Vec2<int> result{0};
                SDL_GetWindowSize(_window, &result.x, &result.y);
                onResize->Invoke(this, result);
            }
            break;
        case SDL_EVENT_WINDOW_CLOSE_REQUESTED:
            {
                onCloseRequested->Invoke(this);
            }
            break;
        case SDL_EVENT_WINDOW_MINIMIZED:
            {
                _minimized = true;
                onMinimized->Invoke(this);
            }
            break;
        case SDL_EVENT_WINDOW_MAXIMIZED:
            {
                _minimized = false;
                onMaximized->Invoke(this);
            }
            break;
        case SDL_EVENT_WINDOW_HIDDEN:
            {
                _hidden = true;
                onHidden->Invoke(this);
            }
            break;
        case SDL_EVENT_WINDOW_SHOWN:
            {
                _hidden = false;
                onShown->Invoke(this);
            }
            break;
        case SDL_EVENT_TEXT_INPUT:
            {
                //event.text.timestamp
            }
            break;
        }
    }

    Window::Window(SDL_Window* inWindow)
    {
        _window = inWindow;
        if (const auto windowModule = GetWindowModule())
        {
            windowModule->onWindowCreated->Invoke(this);
        }
    }

    Window::~Window()
    {
    }

    SDL_Window* Window::GetSDLWindow() const
    {
        return _window;
    }

bool Window::IsMinimized() const
{
    return _minimized;
}

bool Window::IsHidden() const
{
    return _hidden;
}

Vec2<int> Window::GetSize() const
    {
        Vec2<int> result{0};
        SDL_GetWindowSize(_window, &result.x, &result.y);
        return result;
    }

    Vec2<int> Window::GetPosition() const
    {
        Vec2<int> result{0};
        SDL_GetWindowPosition(_window, &result.x, &result.y);
        return result;
    }

    Vec2<float> Window::GetCursorPosition() const
    {
        auto winPosition = GetPosition();
        Vec2<float> position{0.0};
        SDL_GetGlobalMouseState(&position.x, &position.y);
        return position - winPosition.Cast<float>();
    }

    void Window::OnDispose(bool manual)
    {
        RinBase::OnDispose(manual);
        if (const auto windowModule = GetWindowModule())
        {
            windowModule->onWindowDestroyed->Invoke(this);
        }
        SDL_DestroyWindow(_window);
        _window = nullptr;
    }

    WindowModule* Window::GetWindowModule()
    {
        return GRuntime::Get()->GetModule<WindowModule>();
    }
