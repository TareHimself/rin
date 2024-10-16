#include "rin/window/WindowCreateOptions.hpp"
#include <SDL3/SDL_video.h>

int WindowCreateOptions::Apply() const
{
    int flags = SDL_WINDOW_VULKAN;
    if (_resizable) flags |= SDL_WINDOW_RESIZABLE;
    if (_borderless) flags |= SDL_WINDOW_BORDERLESS;
    if (_focused) flags |= SDL_WINDOW_INPUT_FOCUS | SDL_WINDOW_MOUSE_FOCUS;
    if(_tooltip) flags |= SDL_WINDOW_TOOLTIP;
    if(_tooltip) flags |= SDL_WINDOW_TOOLTIP;
    if(_popup) flags |= SDL_WINDOW_POPUP_MENU;
    return flags;
}

WindowCreateOptions& WindowCreateOptions::Resizable(const bool newState)
{
    _resizable = newState;
    return *this;
}

WindowCreateOptions& WindowCreateOptions::Borderless(const bool newState)
{
    _borderless = newState;
    return *this;
}

WindowCreateOptions& WindowCreateOptions::Focused(const bool newState)
{
    _focused = newState;
    return *this;
}

WindowCreateOptions& WindowCreateOptions::Tooltip(const bool newState)
{
    _tooltip = newState;
    return *this;
}

WindowCreateOptions& WindowCreateOptions::Popup(const bool newState)
{
    _popup = newState;
    return *this;
}
