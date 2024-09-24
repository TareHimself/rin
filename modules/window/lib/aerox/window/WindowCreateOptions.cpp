#include "aerox/window/WindowCreateOptions.hpp"
#include <SDL3/SDL_video.h>

int WindowCreateOptions::Apply() const
{
    int flags = SDL_WINDOW_VULKAN;
    if(_resizable) flags |= SDL_WINDOW_RESIZABLE;
    if(_borderless) flags |= SDL_WINDOW_BORDERLESS;
    if(_focused) flags |= SDL_WINDOW_INPUT_FOCUS | SDL_WINDOW_MOUSE_FOCUS;
    return flags;
}

WindowCreateOptions& WindowCreateOptions::Resizable(bool newState)
{
    _resizable = newState;
    return *this;
}

WindowCreateOptions& WindowCreateOptions::Borderless(bool newState)
{
    _borderless = newState;
    return *this;
}

WindowCreateOptions& WindowCreateOptions::Focused(bool newState)
{
    _focused = newState;
    return *this;
}
