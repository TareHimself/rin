#include "platform.hpp"
#include "rwin/rwin.h"

#include <iostream>

#ifdef RIN_PLATFORM_WIN
#include <string>
#include <shlobj.h>
#include <combaseapi.h>
int platformGet()
{
    return static_cast<int>(EPlatform::Windows);
}

void platformInit()
{
    CoInitializeEx(nullptr,COINIT::COINIT_MULTITHREADED);
}

void platformShutdown()
{

}

void platformSelectFile(const char* title, bool multiple, const char* filter, PathReceivedCallback callback)
{
    IFileOpenDialog* fd;
    if(SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,nullptr,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd))))
    {
        DWORD options;

        if(multiple && SUCCEEDED(fd->GetOptions(&options)))
        {
            fd->SetOptions(options | FOS_ALLOWMULTISELECT);
        }
        std::string sTitle(title);
        fd->SetTitle(std::wstring(sTitle.begin(),sTitle.end()).c_str());

        std::string sFilter(filter);

        if(!sFilter.empty())
        {
            const std::wstring extensions(sFilter.begin(),sFilter.end());

            COMDLG_FILTERSPEC rgSpec[] =
            {
                {L"Filter", extensions.c_str()},
                {L"All", L"*.*"},
            };
            fd->SetFileTypes(ARRAYSIZE(rgSpec),rgSpec);
        }
        else
        {
            COMDLG_FILTERSPEC rgSpec[] =
            {
                {L"All", L"*.*"},
            };
            fd->SetFileTypes(ARRAYSIZE(rgSpec),rgSpec);
        }

        if(SUCCEEDED(fd->Show(NULL)))
        {

            if(multiple)
            {
                IShellItemArray* sia;
                if(SUCCEEDED(fd->GetResults(&sia)))
                {
                    DWORD count = 0;
                    if(SUCCEEDED(sia->GetCount(&count)))
                    {
                        for(DWORD i = 0; i < count; i++)
                        {
                            IShellItem* si;
                            if(SUCCEEDED(sia->GetItemAt(i,&si)))
                            {
                                PWSTR filePath = nullptr;

                                if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                                {
                                    std::wstring wstr(filePath);
                                    const int sizeNeeded = WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),nullptr,0,nullptr,nullptr);
                                    std::string strTo(sizeNeeded,0);
                                    WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),
                                                        strTo.data(),sizeNeeded,nullptr,nullptr);
                                    callback(strTo.c_str());
                                    CoTaskMemFree(filePath);
                                }

                                si->Release();
                            }
                        }
                    }
                    sia->Release();
                }
            }
            else
            {
                IShellItem* si;
                if(SUCCEEDED(fd->GetResult(&si)))
                {
                    PWSTR filePath = nullptr;

                    if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                    {
                        const std::wstring wstr(filePath);
                        const int sizeNeeded = WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),nullptr,0,nullptr,nullptr);
                        std::string strTo(sizeNeeded,0);
                        WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),
                                            strTo.data(),sizeNeeded,nullptr,nullptr);
                        callback(strTo.c_str());
                        CoTaskMemFree(filePath);
                    }

                    si->Release();
                }

            }

        }

        fd->Release();
    }
}

void platformSelectPath(const char* title, bool multiple, PathReceivedCallback callback)
{
    IFileOpenDialog* fd;
    if(SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,NULL,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd))))
    {
        DWORD options;

        if(multiple && SUCCEEDED(fd->GetOptions(&options)))
        {
            fd->SetOptions(options | FOS_ALLOWMULTISELECT | FOS_PICKFOLDERS);
        }
        else if(SUCCEEDED(fd->GetOptions(&options)))
        {
            fd->SetOptions(options | FOS_PICKFOLDERS);
        }

        std::string sTitle(title);
        fd->SetTitle(std::wstring(sTitle.begin(),sTitle.end()).c_str());

        if(SUCCEEDED(fd->Show(NULL)))
        {

            if(multiple)
            {
                IShellItemArray* sia;
                if(SUCCEEDED(fd->GetResults(&sia)))
                {
                    DWORD count = 0;
                    if(SUCCEEDED(sia->GetCount(&count)))
                    {
                        for(DWORD i = 0; i < count; i++)
                        {
                            IShellItem* si;
                            if(SUCCEEDED(sia->GetItemAt(i,&si)))
                            {
                                PWSTR filePath = nullptr;

                                if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                                {
                                    const std::wstring wstr(filePath);
                                    const int sizeNeeded = WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),nullptr,0,nullptr,nullptr);
                                    std::string strTo(sizeNeeded,0);
                                    WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),
                                                        strTo.data(),sizeNeeded,nullptr,nullptr);
                                    callback(strTo.c_str());
                                    CoTaskMemFree(filePath);
                                }

                                si->Release();
                            }
                        }
                    }
                    sia->Release();
                }
            }
            else
            {
                IShellItem* si;
                if(SUCCEEDED(fd->GetResult(&si)))
                {
                    PWSTR filePath = nullptr;

                    if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                    {
                        const std::wstring wstr(filePath);
                        const int sizeNeeded = WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),nullptr,0,nullptr,nullptr);
                        std::string strTo(sizeNeeded,0);
                        WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),
                                            strTo.data(),sizeNeeded,nullptr,nullptr);
                        callback(strTo.c_str());
                        CoTaskMemFree(filePath);
                    }

                    si->Release();
                }

            }

        }

        fd->Release();
    }
}


#endif


#ifdef RIN_PLATFORM_LINUX
#include <xkbcommon/xkbcommon.h>
#include <libdecor.h>
#include <vulkan/vulkan.h>
#include <vulkan/vulkan_wayland.h>
#include <list>
#include <ranges>
#include <sstream>
#include <unistd.h>
#include <unordered_map>
#include <unordered_set>
#include <wayland-client.h>
#include <xdg-shell-client-protocol.h>
#include <linux/input-event-codes.h>
#include <sys/mman.h>

wl_registry *REGISTRY = nullptr;
wl_compositor *COMPOSITOR = nullptr;
wl_display *DISPLAY = nullptr;
xdg_wm_base *SHELL = nullptr;
libdecor *DECOR_CONTEXT = nullptr;
wl_seat *SEAT = nullptr;
wl_keyboard *KEYBOARD = nullptr;
wl_pointer *POINTER = nullptr;

struct WindowHandle {
    wl_surface *surface = nullptr;
    Flags<WindowFlags> flags{};
    Extent2D size{};
    libdecor_frame *frame = nullptr;
    Vector2 cursorPosition{};
};

static xkb_context* XKB_CONTEXT = nullptr;

struct KeyboardInfo {
    xkb_keymap * keymap = nullptr;
    xkb_state * state = nullptr;
    std::unordered_set<InputKey> keysPressed{};
    KeyboardInfo(const char * keymapString,xkb_keymap_format format) {
        keymap = xkb_keymap_new_from_string(
                    XKB_CONTEXT, keymapString,format,
                    XKB_KEYMAP_COMPILE_NO_FLAGS);
        state = xkb_state_new(keymap);
    }

    ~KeyboardInfo() {
        xkb_state_unref(state);
        xkb_keymap_unref(keymap);
    }
};
static std::unordered_map<const wl_surface *, WindowHandle *> SURFACE_TO_HANDLES{};
static std::unordered_map<const wl_keyboard*,KeyboardInfo *> KEYBOARDS{};
static WindowHandle *CURSOR_FOCUSED_HANDLE = nullptr;
static WindowHandle *KEYBOARD_FOCUSED_HANDLE = nullptr;
static std::list<WindowEvent> PENDING_EVENTS = {};



WindowHandle *tryGetWindowFromSurface(const wl_surface *surface) {
    if (const auto result = SURFACE_TO_HANDLES.find(surface); result != SURFACE_TO_HANDLES.end()) {
        return result->second;
    }
    return nullptr;
}

InputKey xkbKeyToInputKey(const xkb_keysym_t key) {
    switch (key) {
        case XKB_KEY_A: case XKB_KEY_a: return InputKey::A;
        case XKB_KEY_B: case XKB_KEY_b: return InputKey::B;
        case XKB_KEY_C: case XKB_KEY_c: return InputKey::C;
        case XKB_KEY_D: case XKB_KEY_d: return InputKey::D;
        case XKB_KEY_E: case XKB_KEY_e: return InputKey::E;
        case XKB_KEY_F: case XKB_KEY_f: return InputKey::F;
        case XKB_KEY_G: case XKB_KEY_g: return InputKey::G;
        case XKB_KEY_H: case XKB_KEY_h: return InputKey::H;
        case XKB_KEY_I: case XKB_KEY_i: return InputKey::I;
        case XKB_KEY_J: case XKB_KEY_j: return InputKey::J;
        case XKB_KEY_K: case XKB_KEY_k: return InputKey::K;
        case XKB_KEY_L: case XKB_KEY_l: return InputKey::L;
        case XKB_KEY_M: case XKB_KEY_m: return InputKey::M;
        case XKB_KEY_N: case XKB_KEY_n: return InputKey::N;
        case XKB_KEY_O: case XKB_KEY_o: return InputKey::O;
        case XKB_KEY_P: case XKB_KEY_p: return InputKey::P;
        case XKB_KEY_Q: case XKB_KEY_q: return InputKey::Q;
        case XKB_KEY_R: case XKB_KEY_r: return InputKey::R;
        case XKB_KEY_S: case XKB_KEY_s: return InputKey::S;
        case XKB_KEY_T: case XKB_KEY_t: return InputKey::T;
        case XKB_KEY_U: case XKB_KEY_u: return InputKey::U;
        case XKB_KEY_V: case XKB_KEY_v: return InputKey::V;
        case XKB_KEY_W: case XKB_KEY_w: return InputKey::W;
        case XKB_KEY_X: case XKB_KEY_x: return InputKey::X;
        case XKB_KEY_Y: case XKB_KEY_y: return InputKey::Y;
        case XKB_KEY_Z: case XKB_KEY_z: return InputKey::Z;

        case XKB_KEY_0: return InputKey::Zero;
        case XKB_KEY_1: return InputKey::One;
        case XKB_KEY_2: return InputKey::Two;
        case XKB_KEY_3: return InputKey::Three;
        case XKB_KEY_4: return InputKey::Four;
        case XKB_KEY_5: return InputKey::Five;
        case XKB_KEY_6: return InputKey::Six;
        case XKB_KEY_7: return InputKey::Seven;
        case XKB_KEY_8: return InputKey::Eight;
        case XKB_KEY_9: return InputKey::Nine;

        case XKB_KEY_F1:  return InputKey::F1;
        case XKB_KEY_F2:  return InputKey::F2;
        case XKB_KEY_F3:  return InputKey::F3;
        case XKB_KEY_F4:  return InputKey::F4;
        case XKB_KEY_F5:  return InputKey::F5;
        case XKB_KEY_F6:  return InputKey::F6;
        case XKB_KEY_F7:  return InputKey::F7;
        case XKB_KEY_F8:  return InputKey::F8;
        case XKB_KEY_F9:  return InputKey::F9;
        case XKB_KEY_F10: return InputKey::F10;
        case XKB_KEY_F11: return InputKey::F11;
        case XKB_KEY_F12: return InputKey::F12;
        case XKB_KEY_F13: return InputKey::F13;
        case XKB_KEY_F14: return InputKey::F14;
        case XKB_KEY_F15: return InputKey::F15;
        case XKB_KEY_F16: return InputKey::F16;
        case XKB_KEY_F17: return InputKey::F17;
        case XKB_KEY_F18: return InputKey::F18;
        case XKB_KEY_F19: return InputKey::F19;
        case XKB_KEY_F20: return InputKey::F20;
        case XKB_KEY_F21: return InputKey::F21;
        case XKB_KEY_F22: return InputKey::F22;
        case XKB_KEY_F23: return InputKey::F23;
        case XKB_KEY_F24: return InputKey::F24;

        case XKB_KEY_space:       return InputKey::Space;
        case XKB_KEY_apostrophe:  return InputKey::Apostrophe;
        case XKB_KEY_comma:       return InputKey::Comma;
        case XKB_KEY_minus:       return InputKey::Minus;
        case XKB_KEY_period:      return InputKey::Period;
        case XKB_KEY_slash:       return InputKey::Slash;
        case XKB_KEY_semicolon:   return InputKey::Semicolon;
        case XKB_KEY_equal:       return InputKey::Equal;
        case XKB_KEY_bracketleft: return InputKey::LeftBracket;
        case XKB_KEY_backslash:   return InputKey::Backslash;
        case XKB_KEY_bracketright:return InputKey::RightBracket;
        case XKB_KEY_grave:       return InputKey::GraveAccent;

        case XKB_KEY_Escape:      return InputKey::Escape;
        case XKB_KEY_Return:      return InputKey::Enter;
        case XKB_KEY_Tab:         return InputKey::Tab;
        case XKB_KEY_BackSpace:   return InputKey::Backspace;
        case XKB_KEY_Insert:      return InputKey::Insert;
        case XKB_KEY_Delete:      return InputKey::Delete;
        case XKB_KEY_Right:       return InputKey::Right;
        case XKB_KEY_Left:        return InputKey::Left;
        case XKB_KEY_Down:        return InputKey::Down;
        case XKB_KEY_Up:          return InputKey::Up;
        case XKB_KEY_Page_Up:     return InputKey::PageUp;
        case XKB_KEY_Page_Down:   return InputKey::PageDown;
        case XKB_KEY_Home:        return InputKey::Home;
        case XKB_KEY_End:         return InputKey::End;

        case XKB_KEY_Caps_Lock:     return InputKey::CapsLock;
        case XKB_KEY_Scroll_Lock:   return InputKey::ScrollLock;
        case XKB_KEY_Num_Lock:      return InputKey::NumLock;
        case XKB_KEY_Print:         return InputKey::PrintScreen;
        case XKB_KEY_Pause:         return InputKey::Pause;

        case XKB_KEY_Shift_L:       return InputKey::LeftShift;
        case XKB_KEY_Shift_R:       return InputKey::RightShift;
        case XKB_KEY_Control_L:     return InputKey::LeftControl;
        case XKB_KEY_Control_R:     return InputKey::RightControl;
        case XKB_KEY_Alt_L:         return InputKey::LeftAlt;
        case XKB_KEY_Alt_R:         return InputKey::RightAlt;
        case XKB_KEY_Super_L:       return InputKey::LeftSuper;
        case XKB_KEY_Super_R:       return InputKey::RightSuper;
        case XKB_KEY_Menu:          return InputKey::Menu;

        default:
            std::cerr << "Unknown keysym: " << key << std::endl;
            return InputKey::Unknown;
    }
}

Flags<InputModifier> getInputModifiers(xkb_state * state) {
    Flags<InputModifier> modifiers{};
    if (xkb_state_mod_name_is_active(state, XKB_MOD_NAME_SHIFT, XKB_STATE_MODS_EFFECTIVE))
        modifiers |= InputModifier::Shift;
    if (xkb_state_mod_name_is_active(state, XKB_MOD_NAME_CTRL, XKB_STATE_MODS_EFFECTIVE))
        modifiers |= InputModifier::Control;
    if (xkb_state_mod_name_is_active(state, XKB_MOD_NAME_ALT, XKB_STATE_MODS_EFFECTIVE))
        modifiers |= InputModifier::Alt;
    if (xkb_state_mod_name_is_active(state, XKB_MOD_NAME_LOGO, XKB_STATE_MODS_EFFECTIVE))
        modifiers |= InputModifier::Super;
    if (xkb_state_led_name_is_active(state, XKB_LED_NAME_CAPS))
        modifiers |= InputModifier::CapsLock;
    if (xkb_state_led_name_is_active(state, XKB_LED_NAME_NUM))
        modifiers |= InputModifier::NumLock;

    return modifiers;
}

static constexpr struct wl_display_listener displayListener = {
    .error = [](void *data,
                struct wl_display *wl_display,
                void *object_id,
                uint32_t code,
                const char *message) {
        std::cerr << message << std::endl;
    }
};

static constexpr struct wl_keyboard_listener keyboardListener = {
    .keymap = [](void *data,
                 struct wl_keyboard *wl_keyboard,
                 uint32_t format,
                 int32_t fd,
                 uint32_t size) {
        if (auto ptr = KEYBOARDS.find(wl_keyboard); ptr != KEYBOARDS.end()) {
            delete ptr->second;
            KEYBOARDS.erase(ptr);
        }

        const auto keymapString = static_cast<char*>(mmap(nullptr, size, PROT_READ, MAP_PRIVATE, fd, 0));

        KEYBOARDS.emplace(wl_keyboard,new KeyboardInfo(keymapString, XKB_KEYMAP_FORMAT_TEXT_V1));
        munmap(keymapString, size);
        close(fd);

    },
    .enter = [](void *data,
                struct wl_keyboard *wl_keyboard,
                uint32_t serial,
                struct wl_surface *surface,
                struct wl_array *keys) {
        if (const auto handle = tryGetWindowFromSurface(surface)) {
            KEYBOARD_FOCUSED_HANDLE = handle;
            WindowEvent ev{};
            new(&ev.keyboardFocus) FocusEvent{
                .type = WindowEventType::KeyboardFocus,
                .handle = handle,
                .focused = 1,
            };
            PENDING_EVENTS.push_back(ev);
        }
    },
    .leave = [](void *data,
                struct wl_keyboard *wl_keyboard,
                uint32_t serial,
                struct wl_surface *surface) {
        if (const auto handle = tryGetWindowFromSurface(surface)) {
            KEYBOARD_FOCUSED_HANDLE = nullptr;
            WindowEvent ev{};
            new(&ev.keyboardFocus) FocusEvent{
                .type = WindowEventType::KeyboardFocus,
                .handle = handle,
                .focused = 0
            };
            PENDING_EVENTS.push_back(ev);
        }
    },
    .key = [](void *data,
              struct wl_keyboard *wl_keyboard,
              uint32_t serial,
              uint32_t time,
              uint32_t key,
              uint32_t state) {
        if (KEYBOARD_FOCUSED_HANDLE == nullptr) return;

        const auto keyboardPtr = KEYBOARDS.find(wl_keyboard);
        if (keyboardPtr == KEYBOARDS.end()) return;

        const auto keyboard = keyboardPtr->second;

        InputState inputState{};
        xkb_key_direction direction{};
        if (state == WL_KEYBOARD_KEY_STATE_PRESSED) {
            inputState = InputState::Pressed;
            direction = xkb_key_direction::XKB_KEY_DOWN;
        }
        else if (state == WL_KEYBOARD_KEY_STATE_RELEASED) {
            inputState = InputState::Released;
            direction = xkb_key_direction::XKB_KEY_UP;
        }

        const auto keyCode = key + 8;
        xkb_state_update_key(keyboard->state, keyCode, direction);

        const auto xkbKey = xkb_state_key_get_one_sym(keyboard->state,keyCode);
        const auto rinKey = xkbKeyToInputKey(xkbKey);

        if (keyboard->keysPressed.contains(rinKey) && inputState == InputState::Pressed) {
            inputState = InputState::Repeat;
        }
        const auto modifiers = getInputModifiers(keyboard->state);
        WindowEvent ev{};
        new(&ev.key) KeyEvent{
            .type = WindowEventType::Key,
            .handle = KEYBOARD_FOCUSED_HANDLE,
            .key = rinKey,
            .state = inputState,
            .modifier = static_cast<InputModifier>(modifiers)
        };
        PENDING_EVENTS.push_back(ev);

        if (keyboard->keysPressed.contains(rinKey) && inputState == InputState::Released) {
            keyboard->keysPressed.erase(rinKey);
        }
        else {
            keyboard->keysPressed.insert(rinKey);
        }
    },
    .modifiers = [](void *data,
                    struct wl_keyboard *wl_keyboard,
                    uint32_t serial,
                    uint32_t mods_depressed,
                    uint32_t mods_latched,
                    uint32_t mods_locked,
                    uint32_t group) {
        auto keyboard = KEYBOARDS[wl_keyboard];

        xkb_state_update_mask(keyboard->state,
        mods_depressed, mods_latched, mods_locked, 0, 0, group);

    },
    .repeat_info = [](void *data,
                      struct wl_keyboard *wl_keyboard,
                      int32_t rate,
                      int32_t delay) {

    },
};

static constexpr struct wl_pointer_listener pointerListener = {
    .enter = [](void *data,
                struct wl_pointer *wl_pointer,
                uint32_t serial,
                struct wl_surface *surface,
                wl_fixed_t surface_x,
                wl_fixed_t surface_y) {
        if (const auto handle = tryGetWindowFromSurface(surface)) {
            CURSOR_FOCUSED_HANDLE = handle;
            WindowEvent ev{};
            new(&ev.cursorFocus) FocusEvent{
                .type = WindowEventType::CursorFocus,
                .handle = handle,
                .focused = 1,
            };
            PENDING_EVENTS.push_back(ev);
        }
    },
    .leave = [](void *data,
                struct wl_pointer *wl_pointer,
                uint32_t serial,
                struct wl_surface *surface) {
        if (const auto handle = tryGetWindowFromSurface(surface)) {
            CURSOR_FOCUSED_HANDLE = nullptr;
            WindowEvent ev{};
            new(&ev.cursorFocus) FocusEvent{
                .type = WindowEventType::CursorFocus,
                .handle = handle,
                .focused = 0,
            };
            PENDING_EVENTS.push_back(ev);
        }
    },
    .motion = [](void *data,
                 struct wl_pointer *wl_pointer,
                 uint32_t time,
                 wl_fixed_t surface_x,
                 wl_fixed_t surface_y) {
        if (const auto handle = CURSOR_FOCUSED_HANDLE) {
            const auto x = static_cast<float>(wl_fixed_to_double(surface_x));
            const auto y = static_cast<float>(wl_fixed_to_double(surface_y));
            WindowEvent ev{};
            handle->cursorPosition = {x, y};

            new(&ev.cursorMove) CursorMoveEvent{
                .type = WindowEventType::CursorMove,
                .handle = handle,
                .position = handle->cursorPosition,
            };
            PENDING_EVENTS.push_back(ev);
        }
    },
    .button = [](void *data,
                 struct wl_pointer *wl_pointer,
                 uint32_t serial,
                 uint32_t time,
                 uint32_t button,
                 uint32_t state) {
        if (const auto handle = CURSOR_FOCUSED_HANDLE) {
            WindowEvent ev{};
            CursorButton btn;
            switch (button) {
                case BTN_LEFT: btn = CursorButton::One;
                    break;
                case BTN_RIGHT: btn = CursorButton::Two;
                    break;
                case BTN_MIDDLE: btn = CursorButton::Three;
                    break;
                case BTN_SIDE: btn = CursorButton::Four;
                    break;
                case BTN_EXTRA: btn = CursorButton::Five;
                    break;
                case BTN_FORWARD: btn = CursorButton::Six;
                    break;
                case BTN_BACK: btn = CursorButton::Seven;
                    break;
                default: return; // unknown button
            }

            const InputState btnState = (state == WL_POINTER_BUTTON_STATE_PRESSED)
                                            ? InputState::Pressed
                                            : InputState::Released;
            new(&ev.cursorButton) CursorButtonEvent{
                .type = WindowEventType::CursorButton,
                .handle = handle,
                .button = btn,
                .state = btnState,
                .modifier = static_cast<InputModifier>(0),
            };
            PENDING_EVENTS.push_back(ev);
        }
    },
    .axis = [](void *data,
               struct wl_pointer *wl_pointer,
               uint32_t time,
               uint32_t axis,
               wl_fixed_t value) {
    },
    .frame = [](void *data,
                struct wl_pointer *wl_pointer) {
    },
    .axis_source = [](void *data,
                      struct wl_pointer *wl_pointer,
                      uint32_t axis_source) {
    },
    .axis_stop = [](void *data,
                    struct wl_pointer *wl_pointer,
                    uint32_t time,
                    uint32_t axis) {
    },
    .axis_discrete = [](void *data,
                        struct wl_pointer *wl_pointer,
                        uint32_t axis,
                        int32_t discrete) {
    },
    .axis_value120 = [](void *data,
                        struct wl_pointer *wl_pointer,
                        uint32_t axis,
                        int32_t value120) {
    },
    .axis_relative_direction = [](void *data,
                                  struct wl_pointer *wl_pointer,
                                  uint32_t axis,
                                  uint32_t direction) {
    },
};

static constexpr struct wl_seat_listener seatListener = {
    .capabilities = [](void *data,
                       struct wl_seat *wl_seat,
                       uint32_t capabilities) {
        if ((capabilities & WL_SEAT_CAPABILITY_KEYBOARD) > 0) {
            KEYBOARD = wl_seat_get_keyboard(SEAT);
            wl_keyboard_add_listener(KEYBOARD, &keyboardListener, nullptr);
        }

        if ((capabilities & WL_SEAT_CAPABILITY_POINTER) > 0) {
            POINTER = wl_seat_get_pointer(SEAT);
            wl_pointer_add_listener(POINTER, &pointerListener, nullptr);
        }
    },
    .name = [](void *data,
               struct wl_seat *wl_seat,
               const char *name) {
    }
};
static constexpr struct wl_registry_listener registryListener = {
    .global = [](
void *data,
struct wl_registry *registry,
uint32_t name,
const char *interface,
uint32_t version
) {
        auto interfaceName = std::string{interface};

        if (interfaceName == wl_compositor_interface.name) {
            const auto bindVersion = std::min<uint32_t>(version, wl_seat_interface.version);
            COMPOSITOR = static_cast<wl_compositor *>(wl_registry_bind(registry, name, &wl_compositor_interface,
                                                                       bindVersion));
        } else if (interfaceName == wl_seat_interface.name) {
            const auto bindVersion = std::min<uint32_t>(version, wl_seat_interface.version);
            SEAT = static_cast<wl_seat *>(wl_registry_bind(registry, name, &wl_seat_interface, bindVersion));
            wl_seat_add_listener(SEAT, &seatListener, nullptr);
        }
    }
};

static struct libdecor_frame_interface frameInterface{
    .configure = [](struct libdecor_frame *frame,
                    struct libdecor_configuration *configuration,
                    void *user_data) {
        auto handle = static_cast<WindowHandle *>(user_data);

        int width, height;
        if (libdecor_configuration_get_content_size(configuration, frame, &width, &height)) {
            if (width == 0 || height == 0) {
                // Compositor is asking us to size ourself

                return;
            }

            auto newExtent = Extent2D{static_cast<uint32_t>(width), static_cast<uint32_t>(height)};

            auto state = libdecor_state_new(newExtent.width, newExtent.height);
            libdecor_frame_commit(frame, state, configuration);
            libdecor_state_free(state);

            if (newExtent != handle->size) {
                handle->size = newExtent;
                WindowEvent ev{};
                new(&ev.resize) ResizeEvent{
                    .type = WindowEventType::Resize,
                    .handle = handle,
                    .size = handle->size,
                };
                PENDING_EVENTS.push_back(ev);
            }
        }
    },
    .close = [](struct libdecor_frame *frame, void *user_data) {
        auto handle = static_cast<WindowHandle *>(user_data);
        WindowEvent ev{};
        new(&ev.close) CloseEvent{
            .type = WindowEventType::Close,
            .handle = handle,
        };
        PENDING_EVENTS.push_back(ev);
    },
    .commit = [](struct libdecor_frame *frame, void *user_data) {
        auto handle = static_cast<WindowHandle *>(user_data);
        wl_surface_commit(handle->surface);
    }
};

static struct libdecor_interface decorInterface = {
    .error = [](struct libdecor *context,
                enum libdecor_error error,
                const char *message) {
        std::cerr << "Caught error (" << error << "): " << message << std::endl;
        exit(EXIT_FAILURE);
    }
};

void platformInit() {
    XKB_CONTEXT = xkb_context_new(static_cast<xkb_context_flags>(0));
    DISPLAY = wl_display_connect(nullptr);
    //wl_display_add_listener(DISPLAY,&displayListener,nullptr); // errors out with display already has a listener ?
    REGISTRY = wl_display_get_registry(DISPLAY);
    wl_registry_add_listener(REGISTRY, &registryListener, nullptr);
    wl_display_roundtrip(DISPLAY);
    DECOR_CONTEXT = libdecor_new(DISPLAY, &decorInterface);
}

void platformShutdown() {
    if (KEYBOARD) wl_keyboard_destroy(KEYBOARD);
    if (POINTER) wl_pointer_destroy(POINTER);
    if (SEAT) wl_seat_destroy(SEAT);
    if (DECOR_CONTEXT) libdecor_unref(DECOR_CONTEXT);
    if (COMPOSITOR) wl_compositor_destroy(COMPOSITOR);
    if (REGISTRY) wl_registry_destroy(REGISTRY);
    if (DISPLAY) wl_display_disconnect(DISPLAY);
    for (const auto keyboard: KEYBOARDS | std::views::values) {
        delete keyboard;
    }
    KEYBOARDS.clear();
    if (XKB_CONTEXT) xkb_context_unref(XKB_CONTEXT);
}

int platformGet() {
    return static_cast<int>(EPlatform::Linux);
}

static void runZenityCommand(std::ostringstream& command, bool multiple,const char* filter, PathReceivedCallback callback) {
    if (filter) {
        std::string filterString{filter};
        if (!filterString.empty()) {
            std::stringstream filterStream{filter};
            std::string parsedFilter;
            command << " --file-filter=\"";

            while (std::getline(filterStream,parsedFilter,';')) {
                if (!parsedFilter.empty()) {
                    command << " " << parsedFilter;
                }
            }

            command << "\"";
        }
    }

    command << " 2> /dev/null";

    auto cmd = command.str();
    FILE *pipe = popen(command.str().c_str(), "r");
    if (!pipe) return;

    std::string result;
    char buffer[4096];
    while (fgets(buffer, sizeof(buffer), pipe)) {
        result += buffer;
    }
    pclose(pipe);

    // Remove trailing newline
    if (!result.empty() && result.back() == '\n')
        result.pop_back();

    if (multiple) {
        std::stringstream ss(result);
        std::string path;
        while (std::getline(ss, path, ':')) {
            if (!path.empty())
                callback(path.c_str());
        }
    } else {
        if (!result.empty())
            callback(result.c_str());
    }
}

void platformSelectFile(const char *title, bool multiple, const char *filter,
                                    PathReceivedCallback callback) {
    std::ostringstream cmd;
    cmd << "zenity --file-selection";
    if (multiple)
        cmd << " --multiple --separator=\":\"";
    if (title)
        cmd << " --title=\"" << title << "\"";

    runZenityCommand(cmd, multiple,filter, callback);
}

void platformSelectPath(const char *title, bool multiple, PathReceivedCallback callback) {
    std::ostringstream cmd;
    cmd << "zenity --file-selection --directory";
    if (multiple)
        cmd << " --multiple --separator=\":\"";
    if (title)
        cmd << " --title=\"" << title << "\"";

    runZenityCommand(cmd, multiple,nullptr, callback);
}

void platformWindowPump() {
    wl_display_roundtrip(DISPLAY);
}

void *platformWindowCreate(const char *title, int width, int height, Flags<WindowFlags> flags) {
    auto windowHandle = new WindowHandle{};
    auto surface = wl_compositor_create_surface(COMPOSITOR);
    auto frame = libdecor_decorate(DECOR_CONTEXT, surface, &frameInterface, windowHandle);
    SURFACE_TO_HANDLES.insert_or_assign(surface, windowHandle);
    windowHandle->surface = surface;
    windowHandle->flags = flags;
    windowHandle->size = Extent2D{static_cast<uint32_t>(width), static_cast<uint32_t>(height)};
    windowHandle->frame = frame;

    libdecor_frame_set_title(frame, title);
    libdecor_frame_set_app_id(frame, "rin_app");
    libdecor_frame_map(frame);

    return windowHandle;
}

void platformWindowDestroy(void *handle) {
    if (handle == CURSOR_FOCUSED_HANDLE) {
        CURSOR_FOCUSED_HANDLE = nullptr;
    }
    if (handle == KEYBOARD_FOCUSED_HANDLE) {
        KEYBOARD_FOCUSED_HANDLE = nullptr;
    }
    const auto windowHandle = static_cast<WindowHandle *>(handle);
    SURFACE_TO_HANDLES.erase(windowHandle->surface);
    libdecor_frame_unref(windowHandle->frame);
    wl_surface_destroy(windowHandle->surface);
    delete windowHandle;
}

void platformWindowShow(void *handle) {
}

void platformWindowHide(void *handle) {
}

Vector2 platformWindowGetCursorPosition(void *handle) {
    auto windowHandle = static_cast<WindowHandle *>(handle);
    return windowHandle->cursorPosition;
}

void platformWindowSetCursorPosition(void *handle, Vector2 position) {
}

Extent2D platformWindowGetSize(void *handle) {
    auto windowHandle = static_cast<WindowHandle *>(handle);
    return windowHandle->size;
}

VkSurfaceKHR platformWindowCreateSurface(VkInstance instance, void *handle) {
    auto windowHandle = static_cast<WindowHandle *>(handle);

    const VkWaylandSurfaceCreateInfoKHR createInfo{
        .sType = VK_STRUCTURE_TYPE_WAYLAND_SURFACE_CREATE_INFO_KHR,
        .display = DISPLAY,
        .surface = windowHandle->surface
    };

    VkSurfaceKHR surface{};
    vkCreateWaylandSurfaceKHR(instance, &createInfo, nullptr, &surface);
    return surface;
}

int platformWindowGetEvents(WindowEvent *output, int size) {
    int events = 0;
    for (auto i = 0; i < size; i++) {
        if (PENDING_EVENTS.empty()) {
            break;
        }

        auto event = PENDING_EVENTS.front();
        PENDING_EVENTS.pop_front();
        output[i] = event;
        events++;
    }
    return events;
}

void platformWindowStartTyping(void *handle) {
}

void platformWindowStopTyping(void *handle) {
}

void platformWindowSetSize(void *handle, Extent2D size) {

}
#endif


#ifdef RIN_PLATFORM_MAC

void platformInit()
{

}
int platformGet()
{
    return static_cast<int>(EPlatform::Mac);
}

void platformInit()
{

}
int platformGet()
{
    return static_cast<int>(EPlatform::Linux);
}
void platformSelectFile(const char* title, bool multiple, const char* filter, PathReceivedCallback callback)
{

}
void platformSelectPath(const char* title, bool multiple, PathReceivedCallback callback)
{

}
#endif

void platformWindowPump()
{
    rwin::pumpEvents();
}

std::uint64_t platformWindowCreate(const char* title, rwin::Extent2D extent, rwin::Flags<rwin::WindowFlags> flags)
{
    const std::string_view titleView{title};
    return rwin::createWindow(titleView,extent,flags);
}

void platformWindowDestroy(std::uint64_t handle)
{
    rwin::destroyWindow(handle);
}

void platformWindowShow(std::uint64_t handle)
{
    rwin::showWindow(handle);
}

void platformWindowHide(std::uint64_t handle)
{
    rwin::hideWindow(handle);
}

rwin::Vector2 platformWindowGetCursorPosition(std::uint64_t handle)
{
    return rwin::getCursorPosition(handle);
}

void platformWindowSetCursorPosition(std::uint64_t handle, rwin::Vector2 position)
{

}

rwin::Extent2D platformWindowGetSize(std::uint64_t handle)
{
    return rwin::getWindowClientSize(handle);
}

VkSurfaceKHR platformWindowCreateSurface(VkInstance instance, std::uint64_t handle)
{
    return rwin::createSurface(handle,instance);
}

int platformWindowGetEvents(rwin::WindowEvent* output, int size)
{
    return static_cast<int>(rwin::getEvents({output,static_cast<std::uint64_t>(size)}));
}

void platformWindowStartTyping(std::uint64_t handle)
{

}

void platformWindowStopTyping(std::uint64_t handle)
{
}

void platformWindowSetPosition(std::uint64_t handle, rwin::Point2D position)
{

}

void platformWindowSetSize(std::uint64_t handle, rwin::Extent2D size)
{

}
