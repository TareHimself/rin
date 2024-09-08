#pragma once
#include <GLFW/glfw3.h>

namespace aerox::window
{
    enum class ECursorButton
    {
        One = GLFW_MOUSE_BUTTON_1,
        Two = GLFW_MOUSE_BUTTON_2,
        Three = GLFW_MOUSE_BUTTON_3,
        Four = GLFW_MOUSE_BUTTON_4,
        Five = GLFW_MOUSE_BUTTON_5,
        Six = GLFW_MOUSE_BUTTON_6,
        Seven = GLFW_MOUSE_BUTTON_7,
        Eight = GLFW_MOUSE_BUTTON_8,
        Left = GLFW_MOUSE_BUTTON_LEFT,
        Right = GLFW_MOUSE_BUTTON_RIGHT,
        Middle = GLFW_MOUSE_BUTTON_MIDDLE,
    };
}
