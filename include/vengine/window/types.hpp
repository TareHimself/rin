#pragma once
#include <string>
#include <GLFW/glfw3.h>

namespace vengine::window {

enum EKey {
  Key_0 = GLFW_KEY_0,
  Key_1 = GLFW_KEY_1,
  Key_2 = GLFW_KEY_2,
  Key_3 = GLFW_KEY_3,
  Key_4 = GLFW_KEY_4,
  Key_5 = GLFW_KEY_5,
  Key_6 = GLFW_KEY_6,
  Key_7 = GLFW_KEY_7,
  Key_8 = GLFW_KEY_8,
  Key_9 = GLFW_KEY_9,
  Key_A = GLFW_KEY_A,
  Key_B = GLFW_KEY_B,
  Key_C = GLFW_KEY_C,
  Key_D = GLFW_KEY_D,
  Key_E = GLFW_KEY_E,
  Key_F = GLFW_KEY_F,
  Key_G = GLFW_KEY_G,
  Key_H = GLFW_KEY_H,
  Key_I = GLFW_KEY_I,
  Key_J = GLFW_KEY_J,
  Key_K = GLFW_KEY_K,
  Key_L = GLFW_KEY_L,
  Key_M = GLFW_KEY_M,
  Key_N = GLFW_KEY_N,
  Key_O = GLFW_KEY_O,
  Key_P = GLFW_KEY_P,
  Key_Q = GLFW_KEY_Q,
  Key_R = GLFW_KEY_R,
  Key_S = GLFW_KEY_S,
  Key_T = GLFW_KEY_T,
  Key_U = GLFW_KEY_U,
  Key_V = GLFW_KEY_V,
  Key_W = GLFW_KEY_W,
  Key_X = GLFW_KEY_X,
  Key_Y = GLFW_KEY_Y,
  Key_Z = GLFW_KEY_Z,
  Key_F1 = GLFW_KEY_F1,
  Key_F2 = GLFW_KEY_F2,
  Key_F3 = GLFW_KEY_F3,
  Key_F4 = GLFW_KEY_F4,
  Key_F5 = GLFW_KEY_F5,
  Key_F6 = GLFW_KEY_F6,
  Key_F7 = GLFW_KEY_F7,
  Key_F8 = GLFW_KEY_F8,
  Key_F9 = GLFW_KEY_F9,
  Key_F10 = GLFW_KEY_F10,
  Key_F11 = GLFW_KEY_F11,
  Key_F12 = GLFW_KEY_F12,
  Key_Space = GLFW_KEY_SPACE,
  Key_Apostrophe = GLFW_KEY_APOSTROPHE,
  Key_Comma = GLFW_KEY_COMMA,
  Key_Minus = GLFW_KEY_MINUS,
  Key_Period = GLFW_KEY_PERIOD,
  Key_Slash = GLFW_KEY_SLASH,
  Key_KP_Decimal = GLFW_KEY_KP_DECIMAL,
  Key_KP_Divide = GLFW_KEY_KP_DIVIDE,
  Key_KP_Multiply = GLFW_KEY_KP_MULTIPLY,
  Key_KP_Subtract = GLFW_KEY_KP_SUBTRACT,
  Key_KP_Add = GLFW_KEY_KP_ADD,
  Key_KP_Enter = GLFW_KEY_KP_ENTER,
  Key_KP_Equal = GLFW_KEY_KP_EQUAL,
  Key_LeftShift = GLFW_KEY_LEFT_SHIFT,
  Key_LeftControl = GLFW_KEY_LEFT_CONTROL,
  Key_LeftAlt = GLFW_KEY_LEFT_ALT,
  Key_LeftSuper = GLFW_KEY_LEFT_SUPER,
  Key_RightShift = GLFW_KEY_RIGHT_SHIFT,
  Key_RightControl = GLFW_KEY_RIGHT_CONTROL,
  Key_RightAlt = GLFW_KEY_RIGHT_ALT,
  Key_RightSuper = GLFW_KEY_RIGHT_SUPER,
  Key_Escape = GLFW_KEY_ESCAPE,
};

enum EMouseButton {
  MouseButton_1 = GLFW_MOUSE_BUTTON_1,
  MouseButton_2 = GLFW_MOUSE_BUTTON_2,
  MouseButton_3 = GLFW_MOUSE_BUTTON_3,
  MouseButton_4 = GLFW_MOUSE_BUTTON_4,
  MouseButton_5 = GLFW_MOUSE_BUTTON_5,
  MouseButton_6 = GLFW_MOUSE_BUTTON_6,
  MouseButton_7 = GLFW_MOUSE_BUTTON_7,
  MouseButton_8 = GLFW_MOUSE_BUTTON_8,
  MouseButton_Left = GLFW_MOUSE_BUTTON_LEFT,
  MouseButton_Right = GLFW_MOUSE_BUTTON_RIGHT,
  MouseButton_Middle = GLFW_MOUSE_BUTTON_MIDDLE,
};

enum ECursorMode {
  CursorMode_Visible = GLFW_CURSOR_NORMAL,
  CursorMode_HiddenOverWindow = GLFW_CURSOR_HIDDEN,
  CursorMode_Captured = GLFW_CURSOR_DISABLED
};


struct KeyEvent {
  EKey key;
  operator std::string() const;

  KeyEvent(EKey inKey);
};

struct MouseMovedEvent {
  double x;
  double y;
  operator std::string() const;

  MouseMovedEvent(double inX, double inY);
};

struct MouseButtonEvent {
  EMouseButton button;
  double x;
  double y;
  operator std::string() const;

  MouseButtonEvent(EMouseButton inButton, double inX, double inY);
};

struct ScrollEvent {
  double x;
  double y;
  double dx;
  double dy;
  operator std::string() const;

  ScrollEvent(double inX, double inY,double inDx, double inDy);
};

}
