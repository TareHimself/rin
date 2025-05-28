using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Engine.Graphics.Windows.Events;
using Rin.Engine.Math;
using SDL;
using static SDL.SDL3;

namespace Rin.Engine.Graphics.Windows;

public class SdlWindow : IWindow
{
    private readonly HashSet<IWindow> _children = [];

    private readonly unsafe SDL_Window* _nativePtr;


    private readonly List<string> _pendingDropPaths = [];
    private readonly List<string> _pendingDropTexts = [];
    private Vector2 _cursorPosition = Vector2.Zero;

    //public Vec2<uint> PixelSize;
    private unsafe IntPtr _ptr;
    public unsafe SdlWindow(SDL_Window* windowPointer, IWindow? parent)
    {
        _ptr = Native.Platform.Window.Create("Very Long Window Name", 500, 500,WindowFlags.Resizable);
        Native.Platform.Window.Show(_ptr);
        _nativePtr = windowPointer;
        Parent = parent;
        // _keyDelegate = KeyCallback;
        // _cursorDelegate = CursorCallback;
        // _mouseButtonDelegate = MouseButtonCallback;
        // _focusDelegate = FocusCallback;
        // _scrollDelegate = ScrollCallback;
        // _sizeDelegate = SizeCallback;
        // _closeDelegate = CloseCallback;
        // _charDelegate = CharCallback;
        // _maximizedDelegate = MaximizedCallback;
        // _refreshDelegate = RefreshCallback;
        // _minimizeDelegate = MinimizeCallback;
        // _dropDelegate = DropCallback;
        // _nativePtr = nativePtr;
        // PixelSize = GetPixelSize();
        // NativeMethods.SetWindowCallbacks(_nativePtr,
        //     _keyDelegate,
        //     _cursorDelegate,
        //     _mouseButtonDelegate,
        //     _focusDelegate,
        //     _scrollDelegate,
        //     _sizeDelegate,
        //     _closeDelegate,
        //     _charDelegate,
        //     _maximizedDelegate,
        //     _refreshDelegate,
        //     _minimizeDelegate,
        //     _dropDelegate);
    }

    public IWindow? Parent { get; }

    public bool Focused { get; private set; }

    public bool IsFullscreen
    {
        get
        {
            unsafe
            {
                return (SDL_GetWindowFlags(_nativePtr) & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0;
            }
        }
    }

    public event Action? OnDispose;

    public event Action<KeyEvent>? OnKey;
    public event Action<CursorMoveEvent>? OnCursorMoved;
    public event Action<CursorButtonEvent>? OnCursorButton;
    public event Action<CursorEvent>? OnCursorEnter;
    public event Action<WindowEvent>? OnCursorLeave;
    public event Action<FocusEvent>? OnFocus;
    public event Action<ScrollEvent>? OnScroll;
    public event Action<ResizeEvent>? OnResize;
    public event Action<CloseEvent>? OnClose;
    public event Action<CharacterEvent>? OnCharacter;

    public event Action<MaximizeEvent>? OnMaximize;

    public event Action<RefreshEvent>? OnRefresh;

    public event Action<MinimizeEvent>? OnMinimize;

    public event Action<DropEvent>? OnDrop;

    public void SetHitTestCallback(Func<WindowHitTestResult, IWindow>? callback)
    {
        throw new NotImplementedException();
    }

    public Vector2 GetCursorPosition()
    {
        return _cursorPosition;
    }

    public void SetCursorPosition(in Vector2 position)
    {
        //NativeMethods.SetMousePosition(_nativePtr, position.X, position.Y);
    }

    public void SetFullscreen(bool state)
    {
        unsafe
        {
            SDL_SetWindowFullscreen(_nativePtr, state);
        }
        //NativeMethods.SetWindowFullscreen(_nativePtr, state ? 1 : 0);
    }

    public void SetSize(in Extent2D size)
    {
        unsafe
        {
            //SDL_SetWindowSize(_nativePtr, size, height);
        }
    }

    public void SetPosition(in Offset2D position)
    {
        unsafe
        {
            //SDL_SetWindowPosition(_nativePtr, position, y);
        }
    }


    public WindowRect GetDrawRect()
    {
        WindowRect result = default;
        unsafe
        {
            //SDL_GetWindowSizeInPixels(_nativePtr, &result.Extent.X, &result.Extent.Y);
            //NativeMethods.GetWindowPixelSize(_nativePtr, &result.X, &result.Y);
        }

        return result;
    }

    public WindowRect GetRect()
    {
        throw new NotImplementedException();
    }

    public nuint GetPtr()
    {
        unsafe
        {
            return (nuint)_nativePtr;
        }
        //return _nativePtr;
    }

    public IWindow CreateChild(int width, int height, string name, WindowFlags flags = WindowFlags.Visible)
    {
        throw new NotImplementedException();
        // var child = SGraphicsModule.Get().CreateWindow(width, height, name, options, this);
        // _children.Add(child);
        // child.OnDispose += () => _children.Remove(child);
        // return child;
    }

    public void StartTyping()
    {
        unsafe
        {
            SDL_StartTextInput(_nativePtr);
        }
    }

    public void StopTyping()
    {
        unsafe
        {
            SDL_StopTextInput(_nativePtr);
        }
    }

    public void Dispose()
    {
        unsafe
        {
            Native.Platform.Window.Destroy(_ptr);
            _ptr = IntPtr.Zero;
        }
        foreach (var window in _children) window.Dispose();

        _children.Clear();

        OnDispose?.Invoke();

        unsafe
        {
            SDL_DestroyWindow(_nativePtr);
        }
    }

    public static InputModifier TranslateSdl3KeyMod(SDL_Keymod keymod)
    {
        InputModifier mods = 0;
        if ((keymod & SDL_Keymod.SDL_KMOD_SHIFT) != 0) mods |= InputModifier.Shift;

        if ((keymod & SDL_Keymod.SDL_KMOD_CTRL) != 0) mods |= InputModifier.Control;

        if ((keymod & SDL_Keymod.SDL_KMOD_ALT) != 0) mods |= InputModifier.Alt;

        if ((keymod & SDL_Keymod.SDL_KMOD_MODE) != 0) mods |= InputModifier.Super;

        if ((keymod & SDL_Keymod.SDL_KMOD_CAPS) != 0) mods |= InputModifier.CapsLock;

        if ((keymod & SDL_Keymod.SDL_KMOD_NUM) != 0) mods |= InputModifier.NumLock;

        return mods;
    }

    public static InputKey TranslateSdl3Key(SDL_Keycode sdlKey)
    {
        switch (sdlKey)
        {
            // Printable keys
            case SDL_Keycode.SDLK_SPACE: return InputKey.Space;
            case SDL_Keycode.SDLK_APOSTROPHE: return InputKey.Apostrophe;
            case SDL_Keycode.SDLK_COMMA: return InputKey.Comma;
            case SDL_Keycode.SDLK_MINUS: return InputKey.Minus;
            case SDL_Keycode.SDLK_PERIOD: return InputKey.Period;
            case SDL_Keycode.SDLK_SLASH: return InputKey.Slash;
            case SDL_Keycode.SDLK_0: return InputKey.Zero;
            case SDL_Keycode.SDLK_1: return InputKey.One;
            case SDL_Keycode.SDLK_2: return InputKey.Two;
            case SDL_Keycode.SDLK_3: return InputKey.Three;
            case SDL_Keycode.SDLK_4: return InputKey.Four;
            case SDL_Keycode.SDLK_5: return InputKey.Five;
            case SDL_Keycode.SDLK_6: return InputKey.Six;
            case SDL_Keycode.SDLK_7: return InputKey.Seven;
            case SDL_Keycode.SDLK_8: return InputKey.Eight;
            case SDL_Keycode.SDLK_9: return InputKey.Nine;
            case SDL_Keycode.SDLK_SEMICOLON: return InputKey.Semicolon;
            case SDL_Keycode.SDLK_EQUALS: return InputKey.Equal;
            case SDL_Keycode.SDLK_A: return InputKey.A;
            case SDL_Keycode.SDLK_B: return InputKey.B;
            case SDL_Keycode.SDLK_C: return InputKey.C;
            case SDL_Keycode.SDLK_D: return InputKey.D;
            case SDL_Keycode.SDLK_E: return InputKey.E;
            case SDL_Keycode.SDLK_F: return InputKey.F;
            case SDL_Keycode.SDLK_G: return InputKey.G;
            case SDL_Keycode.SDLK_H: return InputKey.H;
            case SDL_Keycode.SDLK_I: return InputKey.I;
            case SDL_Keycode.SDLK_J: return InputKey.J;
            case SDL_Keycode.SDLK_K: return InputKey.K;
            case SDL_Keycode.SDLK_L: return InputKey.L;
            case SDL_Keycode.SDLK_M: return InputKey.M;
            case SDL_Keycode.SDLK_N: return InputKey.N;
            case SDL_Keycode.SDLK_O: return InputKey.O;
            case SDL_Keycode.SDLK_P: return InputKey.P;
            case SDL_Keycode.SDLK_Q: return InputKey.Q;
            case SDL_Keycode.SDLK_R: return InputKey.R;
            case SDL_Keycode.SDLK_S: return InputKey.S;
            case SDL_Keycode.SDLK_T: return InputKey.T;
            case SDL_Keycode.SDLK_U: return InputKey.U;
            case SDL_Keycode.SDLK_V: return InputKey.V;
            case SDL_Keycode.SDLK_W: return InputKey.W;
            case SDL_Keycode.SDLK_X: return InputKey.X;
            case SDL_Keycode.SDLK_Y: return InputKey.Y;
            case SDL_Keycode.SDLK_Z: return InputKey.Z;
            case SDL_Keycode.SDLK_LEFTBRACKET: return InputKey.LeftBracket;
            case SDL_Keycode.SDLK_BACKSLASH: return InputKey.Backslash;
            case SDL_Keycode.SDLK_RIGHTBRACKET: return InputKey.RightBracket;
            case SDL_Keycode.SDLK_GRAVE: return InputKey.GraveAccent;

            // Function and control keys
            case SDL_Keycode.SDLK_ESCAPE: return InputKey.Escape;
            case SDL_Keycode.SDLK_RETURN: return InputKey.Enter;
            case SDL_Keycode.SDLK_TAB: return InputKey.Tab;
            case SDL_Keycode.SDLK_BACKSPACE: return InputKey.Backspace;
            case SDL_Keycode.SDLK_INSERT: return InputKey.Insert;
            case SDL_Keycode.SDLK_DELETE: return InputKey.Delete;
            case SDL_Keycode.SDLK_RIGHT: return InputKey.Right;
            case SDL_Keycode.SDLK_LEFT: return InputKey.Left;
            case SDL_Keycode.SDLK_DOWN: return InputKey.Down;
            case SDL_Keycode.SDLK_UP: return InputKey.Up;
            case SDL_Keycode.SDLK_PAGEUP: return InputKey.PageUp;
            case SDL_Keycode.SDLK_PAGEDOWN: return InputKey.PageDown;
            case SDL_Keycode.SDLK_HOME: return InputKey.Home;
            case SDL_Keycode.SDLK_END: return InputKey.End;
            case SDL_Keycode.SDLK_CAPSLOCK: return InputKey.CapsLock;
            case SDL_Keycode.SDLK_SCROLLLOCK: return InputKey.ScrollLock;
            case SDL_Keycode.SDLK_NUMLOCKCLEAR: return InputKey.NumLock;
            case SDL_Keycode.SDLK_PRINTSCREEN: return InputKey.PrintScreen;
            case SDL_Keycode.SDLK_PAUSE: return InputKey.Pause;

            // Function keys (F1-F25)
            case SDL_Keycode.SDLK_F1: return InputKey.F1;
            case SDL_Keycode.SDLK_F2: return InputKey.F2;
            case SDL_Keycode.SDLK_F3: return InputKey.F3;
            case SDL_Keycode.SDLK_F4: return InputKey.F4;
            case SDL_Keycode.SDLK_F5: return InputKey.F5;
            case SDL_Keycode.SDLK_F6: return InputKey.F6;
            case SDL_Keycode.SDLK_F7: return InputKey.F7;
            case SDL_Keycode.SDLK_F8: return InputKey.F8;
            case SDL_Keycode.SDLK_F9: return InputKey.F9;
            case SDL_Keycode.SDLK_F10: return InputKey.F10;
            case SDL_Keycode.SDLK_F11: return InputKey.F11;
            case SDL_Keycode.SDLK_F12: return InputKey.F12;
            case SDL_Keycode.SDLK_F13: return InputKey.F13;
            case SDL_Keycode.SDLK_F14: return InputKey.F14;
            case SDL_Keycode.SDLK_F15: return InputKey.F15;
            case SDL_Keycode.SDLK_F16: return InputKey.F16;
            case SDL_Keycode.SDLK_F17: return InputKey.F17;
            case SDL_Keycode.SDLK_F18: return InputKey.F18;
            case SDL_Keycode.SDLK_F19: return InputKey.F19;
            case SDL_Keycode.SDLK_F20: return InputKey.F20;
            case SDL_Keycode.SDLK_F21: return InputKey.F21;
            case SDL_Keycode.SDLK_F22: return InputKey.F22;
            case SDL_Keycode.SDLK_F23: return InputKey.F23;
            case SDL_Keycode.SDLK_F24: return InputKey.F24;

            default:
                throw new Exception("Unknown sdl key"); // No match found
        }
    }

    public void HandleEvent(in SDL_Event e)
    {
        
        switch (e.Type)
        {
            case SDL_EventType.SDL_EVENT_FIRST:
                break;
            case SDL_EventType.SDL_EVENT_QUIT:
                break;
            case SDL_EventType.SDL_EVENT_TERMINATING:
                break;
            case SDL_EventType.SDL_EVENT_LOW_MEMORY:
                break;
            case SDL_EventType.SDL_EVENT_WILL_ENTER_BACKGROUND:
                break;
            case SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND:
                break;
            case SDL_EventType.SDL_EVENT_WILL_ENTER_FOREGROUND:
                break;
            case SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND:
                break;
            case SDL_EventType.SDL_EVENT_LOCALE_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_SYSTEM_THEME_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_DISPLAY_ORIENTATION:
                break;
            case SDL_EventType.SDL_EVENT_DISPLAY_ADDED:
                break;
            case SDL_EventType.SDL_EVENT_DISPLAY_REMOVED:
                break;
            case SDL_EventType.SDL_EVENT_DISPLAY_MOVED:
                break;
            case SDL_EventType.SDL_EVENT_DISPLAY_DESKTOP_MODE_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_DISPLAY_CURRENT_MODE_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_DISPLAY_CONTENT_SCALE_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_SHOWN:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_HIDDEN:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_MOVED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                // OnResize?.Invoke(new ResizeEvent
                // {
                //     Window = this,
                //     Rect = GetRect(),
                //     DrawRect = GetDrawRect(),
                // });
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_METAL_VIEW_RESIZED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED:
                OnMinimize?.Invoke(new MinimizeEvent
                {
                    Window = this
                });
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED:
                OnMaximize?.Invoke(new MaximizeEvent
                {
                    Window = this
                });
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
                OnCursorEnter?.Invoke(new CursorEvent
                {
                    Window = this,
                    Position = GetCursorPosition()
                });
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                OnCursorLeave?.Invoke(new WindowEvent
                {
                    Window = this
                });
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                Focused = true;
                OnFocus?.Invoke(new FocusEvent
                {
                    Window = this,
                    IsFocused = true
                });
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                Focused = false;
                OnFocus?.Invoke(new FocusEvent
                {
                    Window = this,
                    IsFocused = false
                });
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                OnClose?.Invoke(new CloseEvent
                {
                    Window = this
                });
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_HIT_TEST:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_ICCPROF_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_SAFE_AREA_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_OCCLUDED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_ENTER_FULLSCREEN:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_LEAVE_FULLSCREEN:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_DESTROYED:
                break;
            case SDL_EventType.SDL_EVENT_WINDOW_HDR_STATE_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_KEY_DOWN:
            case SDL_EventType.SDL_EVENT_KEY_UP:
                OnKey?.Invoke(new KeyEvent
                {
                    Window = this,
                    Key = TranslateSdl3Key(e.key.key),
                    Modifiers = TranslateSdl3KeyMod(e.key.mod),
                    State = e.key.down ? e.key.repeat ? InputState.Repeat : InputState.Pressed : InputState.Released
                });
                break;
            case SDL_EventType.SDL_EVENT_TEXT_EDITING:
                break;
            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
            {
                unsafe
                {
                    var data = Marshal.PtrToStringAuto((nint)e.text.text) ?? string.Empty;
                    foreach (var c in data)
                        OnCharacter?.Invoke(new CharacterEvent
                        {
                            Window = this,
                            Data = c,
                            Modifiers = 0
                        });
                }
            }
                break;
            case SDL_EventType.SDL_EVENT_KEYMAP_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_KEYBOARD_ADDED:
                break;
            case SDL_EventType.SDL_EVENT_KEYBOARD_REMOVED:
                break;
            case SDL_EventType.SDL_EVENT_TEXT_EDITING_CANDIDATES:
                break;
            case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
            {
                _cursorPosition = new Vector2(e.motion.x, e.motion.y);
                OnCursorMoved?.Invoke(new CursorMoveEvent
                {
                    Window = this,
                    Position = _cursorPosition
                });
            }
                break;
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                OnCursorButton?.Invoke(new CursorButtonEvent
                {
                    Window = this,
                    Position = GetCursorPosition(),
                    Button = e.button.Button switch
                    {
                        SDLButton.SDL_BUTTON_LEFT => CursorButton.One,
                        SDLButton.SDL_BUTTON_MIDDLE => CursorButton.Three,
                        SDLButton.SDL_BUTTON_RIGHT => CursorButton.Two,
                        SDLButton.SDL_BUTTON_X1 => CursorButton.Four,
                        SDLButton.SDL_BUTTON_X2 => CursorButton.Five,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    Modifiers = 0,
                    State = e.button.down ? InputState.Pressed : InputState.Released
                });
                break;
            case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                OnScroll?.Invoke(new ScrollEvent
                {
                    Window = this,
                    Position = GetCursorPosition(),
                    Delta = new Vector2(e.wheel.x, e.wheel.y)
                });
                break;
            case SDL_EventType.SDL_EVENT_MOUSE_ADDED:
                break;
            case SDL_EventType.SDL_EVENT_MOUSE_REMOVED:
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_BALL_MOTION:
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_HAT_MOTION:
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_BATTERY_UPDATED:
                break;
            case SDL_EventType.SDL_EVENT_JOYSTICK_UPDATE_COMPLETE:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_REMAPPED:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_TOUCHPAD_DOWN:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_TOUCHPAD_MOTION:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_TOUCHPAD_UP:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_SENSOR_UPDATE:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_UPDATE_COMPLETE:
                break;
            case SDL_EventType.SDL_EVENT_GAMEPAD_STEAM_HANDLE_UPDATED:
                break;
            case SDL_EventType.SDL_EVENT_FINGER_DOWN:
                break;
            case SDL_EventType.SDL_EVENT_FINGER_UP:
                break;
            case SDL_EventType.SDL_EVENT_FINGER_MOTION:
                break;
            case SDL_EventType.SDL_EVENT_FINGER_CANCELED:
                break;
            case SDL_EventType.SDL_EVENT_CLIPBOARD_UPDATE:
                break;
            case SDL_EventType.SDL_EVENT_DROP_FILE:
            {
                unsafe
                {
                    _pendingDropTexts.Add(Marshal.PtrToStringUTF8(new IntPtr(e.drop.data)) ?? "");
                }
            }
                break;
            case SDL_EventType.SDL_EVENT_DROP_TEXT:
            {
                unsafe
                {
                    _pendingDropTexts.Add(Marshal.PtrToStringUTF8(new IntPtr(e.drop.data)) ?? "");
                }
            }
                break;
            case SDL_EventType.SDL_EVENT_DROP_BEGIN:
                _pendingDropPaths.Clear();
                _pendingDropTexts.Clear();
                break;
            case SDL_EventType.SDL_EVENT_DROP_COMPLETE:
                OnDrop?.Invoke(new DropEvent
                {
                    Window = this,
                    Paths = _pendingDropPaths.ToArray(),
                    Texts = _pendingDropTexts.ToArray()
                });
                _pendingDropPaths.Clear();
                _pendingDropTexts.Clear();
                
                break;
            case SDL_EventType.SDL_EVENT_DROP_POSITION:
                break;
            case SDL_EventType.SDL_EVENT_AUDIO_DEVICE_ADDED:
                break;
            case SDL_EventType.SDL_EVENT_AUDIO_DEVICE_REMOVED:
                break;
            case SDL_EventType.SDL_EVENT_AUDIO_DEVICE_FORMAT_CHANGED:
                break;
            case SDL_EventType.SDL_EVENT_SENSOR_UPDATE:
                break;
            case SDL_EventType.SDL_EVENT_PEN_PROXIMITY_IN:
                break;
            case SDL_EventType.SDL_EVENT_PEN_PROXIMITY_OUT:
                break;
            case SDL_EventType.SDL_EVENT_PEN_DOWN:
                break;
            case SDL_EventType.SDL_EVENT_PEN_UP:
                break;
            case SDL_EventType.SDL_EVENT_PEN_BUTTON_DOWN:
                break;
            case SDL_EventType.SDL_EVENT_PEN_BUTTON_UP:
                break;
            case SDL_EventType.SDL_EVENT_PEN_MOTION:
                break;
            case SDL_EventType.SDL_EVENT_PEN_AXIS:
                break;
            case SDL_EventType.SDL_EVENT_CAMERA_DEVICE_ADDED:
                break;
            case SDL_EventType.SDL_EVENT_CAMERA_DEVICE_REMOVED:
                break;
            case SDL_EventType.SDL_EVENT_CAMERA_DEVICE_APPROVED:
                break;
            case SDL_EventType.SDL_EVENT_CAMERA_DEVICE_DENIED:
                break;
            case SDL_EventType.SDL_EVENT_RENDER_TARGETS_RESET:
                break;
            case SDL_EventType.SDL_EVENT_RENDER_DEVICE_RESET:
                break;
            case SDL_EventType.SDL_EVENT_RENDER_DEVICE_LOST:
                break;
            case SDL_EventType.SDL_EVENT_PRIVATE0:
                break;
            case SDL_EventType.SDL_EVENT_PRIVATE1:
                break;
            case SDL_EventType.SDL_EVENT_PRIVATE2:
                break;
            case SDL_EventType.SDL_EVENT_PRIVATE3:
                break;
            case SDL_EventType.SDL_EVENT_POLL_SENTINEL:
                break;
            case SDL_EventType.SDL_EVENT_USER:
                break;
            case SDL_EventType.SDL_EVENT_LAST:
                break;
            case SDL_EventType.SDL_EVENT_ENUM_PADDING:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}