#include "platform.hpp"

#include <iostream>

#ifdef RIN_PLATFORM_WIN
#define VK_USE_PLATFORM_WIN32_KHR
#include <list>
#include <vulkan/vulkan.h>
#include <string>
#include <shlobj.h>
#include <combaseapi.h>
#include <stringapiset.h>
#include <dwmapi.h>
#pragma comment (lib, "Dwmapi")

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
static HINSTANCE INSTANCE;
const LPCSTR DEFAULT_WINDOW_CLASS_NAME = "Default Window Class";

static std::wstring stringToWstring(const std::string& str)
{
    return {str.begin(), str.end()};
}

static std::string wstringToString(const std::wstring& wstr)
{
    const int sizeNeeded = WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),nullptr,0,nullptr,nullptr);
    std::string strTo(sizeNeeded,0);
    WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),
                        strTo.data(),sizeNeeded,nullptr,nullptr);
    return strTo;
}

EXPORT_IMPL int platformGet()
{
    return static_cast<int>(EPlatform::Windows);
}

EXPORT_IMPL void platformInit()
{
    INSTANCE = GetModuleHandle(nullptr);
    CoInitializeEx(nullptr,COINIT::COINIT_MULTITHREADED);
    {
        WNDCLASS wc = {
            .lpfnWndProc = WindowProc,
            .hInstance = INSTANCE,
            .lpszClassName = DEFAULT_WINDOW_CLASS_NAME 
        };

        wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
        RegisterClass(&wc);
    }
}

EXPORT_IMPL void platformSelectFile(const char* title, bool multiple, const char* filter, PathReceivedCallback callback)
{
    IFileOpenDialog* fd;
    if(SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,NULL,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd))))
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

EXPORT_IMPL void platformSelectPath(const char* title, bool multiple, PathReceivedCallback callback)
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

EXPORT_IMPL void platformWindowPump()
{
    MSG msg;
    while(PeekMessage(&msg, nullptr, 0, 0, PM_REMOVE) > 0)
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
}

InputKey MapVirtualKeyToInputKey(UINT vk, LPARAM lParam) {
    // Extended key flag from lParam
    bool isExtended = (lParam >> 24) & 0x1;

    switch (vk) {
        // A-Z
        case 'A': return InputKey::A;
        case 'B': return InputKey::B;
        case 'C': return InputKey::C;
        case 'D': return InputKey::D;
        case 'E': return InputKey::E;
        case 'F': return InputKey::F;
        case 'G': return InputKey::G;
        case 'H': return InputKey::H;
        case 'I': return InputKey::I;
        case 'J': return InputKey::J;
        case 'K': return InputKey::K;
        case 'L': return InputKey::L;
        case 'M': return InputKey::M;
        case 'N': return InputKey::N;
        case 'O': return InputKey::O;
        case 'P': return InputKey::P;
        case 'Q': return InputKey::Q;
        case 'R': return InputKey::R;
        case 'S': return InputKey::S;
        case 'T': return InputKey::T;
        case 'U': return InputKey::U;
        case 'V': return InputKey::V;
        case 'W': return InputKey::W;
        case 'X': return InputKey::X;
        case 'Y': return InputKey::Y;
        case 'Z': return InputKey::Z;

        // 0-9
        case '0': return InputKey::Zero;
        case '1': return InputKey::One;
        case '2': return InputKey::Two;
        case '3': return InputKey::Three;
        case '4': return InputKey::Four;
        case '5': return InputKey::Five;
        case '6': return InputKey::Six;
        case '7': return InputKey::Seven;
        case '8': return InputKey::Eight;
        case '9': return InputKey::Nine;

        // Function keys
        case VK_F1:  return InputKey::F1;
        case VK_F2:  return InputKey::F2;
        case VK_F3:  return InputKey::F3;
        case VK_F4:  return InputKey::F4;
        case VK_F5:  return InputKey::F5;
        case VK_F6:  return InputKey::F6;
        case VK_F7:  return InputKey::F7;
        case VK_F8:  return InputKey::F8;
        case VK_F9:  return InputKey::F9;
        case VK_F10: return InputKey::F10;
        case VK_F11: return InputKey::F11;
        case VK_F12: return InputKey::F12;
        case VK_F13: return InputKey::F13;
        case VK_F14: return InputKey::F14;
        case VK_F15: return InputKey::F15;
        case VK_F16: return InputKey::F16;
        case VK_F17: return InputKey::F17;
        case VK_F18: return InputKey::F18;
        case VK_F19: return InputKey::F19;
        case VK_F20: return InputKey::F20;
        case VK_F21: return InputKey::F21;
        case VK_F22: return InputKey::F22;
        case VK_F23: return InputKey::F23;
        case VK_F24: return InputKey::F24;

        // Punctuation and symbols
        case VK_SPACE:      return InputKey::Space;
        case VK_OEM_7:      return InputKey::Apostrophe;
        case VK_OEM_COMMA:  return InputKey::Comma;
        case VK_OEM_MINUS:  return InputKey::Minus;
        case VK_OEM_PERIOD: return InputKey::Period;
        case VK_OEM_2:      return InputKey::Slash;
        case VK_OEM_1:      return InputKey::Semicolon;
        case VK_OEM_PLUS:   return InputKey::Equal;
        case VK_OEM_4:      return InputKey::LeftBracket;
        case VK_OEM_5:      return InputKey::Backslash;
        case VK_OEM_6:      return InputKey::RightBracket;
        case VK_OEM_3:      return InputKey::GraveAccent;

        // Control keys
        case VK_ESCAPE:     return InputKey::Escape;
        case VK_RETURN:     return InputKey::Enter;
        case VK_TAB:        return InputKey::Tab;
        case VK_BACK:       return InputKey::Backspace;
        case VK_INSERT:     return InputKey::Insert;
        case VK_DELETE:     return InputKey::Delete;
        case VK_HOME:       return InputKey::Home;
        case VK_END:        return InputKey::End;
        case VK_PRIOR:      return InputKey::PageUp;
        case VK_NEXT:       return InputKey::PageDown;

        // Arrows
        case VK_LEFT:       return InputKey::Left;
        case VK_RIGHT:      return InputKey::Right;
        case VK_UP:         return InputKey::Up;
        case VK_DOWN:       return InputKey::Down;

        // Locks and pause
        case VK_CAPITAL:    return InputKey::CapsLock;
        case VK_SCROLL:     return InputKey::ScrollLock;
        case VK_NUMLOCK:    return InputKey::NumLock;
        case VK_SNAPSHOT:   return InputKey::PrintScreen;
        case VK_PAUSE:      return InputKey::Pause;

        // Left/Right modifiers
        case VK_SHIFT: {
            // Distinguish LSHIFT / RSHIFT using scan code
            UINT sc = (lParam >> 16) & 0xFF;
            return (sc == 0x36) ? InputKey::RightShift : InputKey::LeftShift;
        }
        case VK_CONTROL:    return isExtended ? InputKey::RightControl : InputKey::LeftControl;
        case VK_MENU:       return isExtended ? InputKey::RightAlt     : InputKey::LeftAlt;
        case VK_LWIN:       return InputKey::LeftSuper;
        case VK_RWIN:       return InputKey::RightSuper;
        case VK_APPS:       return InputKey::Menu;

        default:            return InputKey::A; // Fallback or unknown
    }
}
static std::list<WindowEvent> PENDING_EVENTS = {};

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch(uMsg)
    {
    // case WM_KEYDOWN:
    //     break;
    // case WM_KEYUP:
    //     break;
    case WM_CHAR:
        {
            WindowEvent ev{};
            new(&ev.text) TextEvent{
                .type = WindowEventType::Text,
                .handle = hwnd,
                .text = static_cast<char16_t>(wParam)
            };
            PENDING_EVENTS.push_back(ev);
            return 0;
        }
    case WM_KEYDOWN:
    case WM_KEYUP: {
            WindowEvent evt{};

            // Fill basic event info

            // Convert virtual key to your InputKey enum
            const auto key = MapVirtualKeyToInputKey(static_cast<UINT>(wParam),lParam);

            // State (pressed / released / repeat)
            bool isRepeat = (lParam & KF_REPEAT) != 0;
            const auto state = (uMsg == WM_KEYUP) ? InputState::Released :
                             (isRepeat ? InputState::Repeat : InputState::Pressed);
            // Modifier bitmask
            Flags<InputModifier> modifiers{};
            if (GetKeyState(VK_SHIFT) & KF_UP)     modifiers.Add(InputModifier::Shift);
            if (GetKeyState(VK_CONTROL) &  KF_UP)   modifiers.Add(InputModifier::Control);
            if (GetKeyState(VK_MENU) & KF_UP)      modifiers.Add(InputModifier::Alt);
            if (GetKeyState(VK_LWIN) & KF_UP || GetKeyState(VK_RWIN) & KF_UP)
                modifiers.Add(InputModifier::Super);
            if (GetKeyState(VK_CAPITAL) & 0x0001)   modifiers.Add(InputModifier::CapsLock);
            if (GetKeyState(VK_NUMLOCK) & 0x0001)    modifiers.Add(InputModifier::NumLock);

            evt.key.modifier = static_cast<InputModifier>(modifiers);
            
            WindowEvent ev{};
            new(&ev.key) KeyEvent{
                .type = WindowEventType::Key,
                .handle = hwnd,
                .key = key,
                .state = state,
                .modifier = static_cast<InputModifier>(modifiers),
            };
            PENDING_EVENTS.push_back(ev);
            // Now dispatch or store evt...
            // Example: processEvent(evt);
            return 0;
    }
    case WM_SIZE:
        { 
            WindowEvent ev{};
            new(&ev.resize) ResizeEvent{
                .type = WindowEventType::Resize,
                .handle = hwnd,
                .rect = platformWindowGetRect(hwnd),
                .drawRect = platformWindowGetDrawRect(hwnd)
            };
            if(!PENDING_EVENTS.empty() && PENDING_EVENTS.back().type == WindowEventType::Resize)
            {
                PENDING_EVENTS.back() = ev;
            }
            else
            {
                PENDING_EVENTS.push_back(ev);
            }
        }
        break;
    case WM_CLOSE:
        {
            WindowEvent ev{};
            new(&ev.close) CloseEvent{
                .type = WindowEventType::Close,
                .handle = hwnd,
            };
            PENDING_EVENTS.push_back(ev);
            return 0;
        }
        break;
    }

    return DefWindowProc(hwnd,uMsg,wParam,lParam);
}

EXPORT_IMPL void* platformWindowCreate(const char* title, int width, int height, Flags<WindowFlags> flags)
{
    auto windowFlags = WS_SYSMENU;

    if(flags.Has(WindowFlags::Resizable))
    {
        windowFlags |= WS_SIZEBOX;
    }
    
    if(flags.Has(WindowFlags::Frameless))
    {
        windowFlags |= WS_POPUP;
    }
    else
    {
        windowFlags |= WS_OVERLAPPED | WS_CAPTION | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
    }
    
    const auto screenWidth  = GetSystemMetrics(SM_CXSCREEN);
    const auto screenHeight = GetSystemMetrics(SM_CYSCREEN);
    const auto x = (screenWidth - width) / 2;
    const auto y = (screenHeight - height) / 2;
    const auto hwnd = CreateWindowEx(
        0,
        DEFAULT_WINDOW_CLASS_NAME,
        title,
        windowFlags,
        x,
        y,
        width,
        height,
        nullptr,
        nullptr,
        INSTANCE,
        nullptr
    );

    if(flags.Has(WindowFlags::Focused))
    {
        SetFocus(hwnd);
    }
    {
        DWM_WINDOW_CORNER_PREFERENCE pref = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, &pref, sizeof(pref));
    }

    ShowWindow(hwnd,flags.Has(WindowFlags::Visible) ? SW_SHOW : SW_HIDE);
    return hwnd;
}
EXPORT_IMPL void platformWindowDestroy(void* handle)
{
    const auto asHandle = static_cast<HWND>(handle);
    DestroyWindow(asHandle);
}
EXPORT_IMPL void platformWindowShow(void* handle)
{
    const auto asHandle = static_cast<HWND>(handle);
    ShowWindow(asHandle, SW_SHOW);
}
EXPORT_IMPL void platformWindowHide(void* handle)
{
    const auto asHandle = static_cast<HWND>(handle);
    ShowWindow(asHandle, SW_HIDE);
}
EXPORT_IMPL Vector2 platformWindowGetCursorPosition(void* handle)
{
    const auto hwnd = static_cast<HWND>(handle);

    POINT pt;
    if(GetCursorPos(&pt))
    {
        // Convert from screen coordinates to client (drawable area) coordinates
        ScreenToClient(hwnd,&pt);
        return {
            .x = static_cast<float>(pt.x),
            .y = static_cast<float>(pt.y)
        };
    }

    return {
        .x = 0,
        .y = 0
    };
}
EXPORT_IMPL void platformWindowSetCursorPosition(void* handle, Vector2 position)
{
    const auto [offset, extent] = platformWindowGetDrawRect(handle);
    SetCursorPos(static_cast<int>(offset.x) + static_cast<int>(position.x),static_cast<int>(offset.y) + static_cast<int>(position.y));
}

EXPORT_IMPL WindowRect platformWindowGetRect(void* handle)
{
    RECT rect;

    const auto hwnd = static_cast<HWND>(handle);

    if(GetWindowRect(hwnd,&rect))
    {
        const auto width = rect.right - rect.left;
        const auto height = rect.bottom - rect.top;
        return {
            .position = Point2D{
                .x = static_cast<int>(rect.left),
                .y = static_cast<int>(rect.top)
            },
            .extent = Extent2D{
                .width = static_cast<uint32_t>(width),
                .height = static_cast<uint32_t>(height)
            }
        };
    }

    return {};
}
EXPORT_IMPL WindowRect platformWindowGetDrawRect(void* handle)
{
    RECT rect;

    const auto hwnd = static_cast<HWND>(handle);

    if(GetClientRect(hwnd,&rect))
    {
        const auto width = rect.right - rect.left;
        const auto height = rect.bottom - rect.top;
        return {
            .position = Point2D{
                .x = static_cast<int>(rect.left),
                .y = static_cast<int>(rect.top)
            },
            .extent = Extent2D{
                .width = static_cast<uint32_t>(width),
                .height = static_cast<uint32_t>(height)
            }
        };
    }

    return {};
}

EXPORT_IMPL VkSurfaceKHR platformWindowCreateSurface(VkInstance instance, void* handle)
{
    const auto hwnd = static_cast<HWND>(handle);

    const auto createInfo = VkWin32SurfaceCreateInfoKHR{
        .sType = VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
        .pNext = nullptr,
        .flags = 0,
        .hinstance = INSTANCE,
        .hwnd = hwnd,
    };
    VkSurfaceKHR surface{};
    vkCreateWin32SurfaceKHR(instance,&createInfo,nullptr,&surface);
    return surface;
}

EXPORT_IMPL int platformWindowGetEvents(WindowEvent* output, int size)
{
    int events = 0;
    for(auto i = 0; i < size; i++)
    {
        if(PENDING_EVENTS.empty())
        {
            break;
        }

        auto event = PENDING_EVENTS.front();
        PENDING_EVENTS.pop_front();
        output[i] = event;
        events++;
    }
    return events;
}
EXPORT_IMPL void platformWindowStartTyping(void* handle)
{
    // Not needed on windows
}
EXPORT_IMPL void platformWindowStopTyping(void* handle)
{
    // Not needed on windows
}

EXPORT_IMPL void platformWindowSetSize(void* handle,Extent2D size)
{
/*
    auto hwnd = static_cast<HWND>(handle);
    auto cRect = platformWindowGetDrawRect(handle);
    auto rect = platformWindowGetRect(handle);
    
    SetWindowPos(hwnd,HWND_NOTOPMOST,0,0,)*/
}

EXPORT_IMPL void platformWindowSetPosition(void* handle,Point2D position)
{
    /*auto hwnd = static_cast<HWND>(handle);
    SetWindowPos(hwnd,HWND_NOTOPMOST,position.x,position.y,0,0,SWP_NOREPOSITION);*/
}
#endif


#ifdef RIN_PLATFORM_LINUX
#include <libdecor.h>
#include <vulkan/vulkan.h>
#include <vulkan/vulkan_wayland.h>
#include <list>
#include <sstream>
#include <unordered_map>
#include <wayland-client.h>
#include <xdg-shell-client-protocol.h>
#include <linux/input-event-codes.h>

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

static std::unordered_map<const wl_surface *, WindowHandle *> SURFACE_TO_HANDLES{};
static WindowHandle *FOCUSED_HANDLE = nullptr;
static std::list<WindowEvent> PENDING_EVENTS = {};

WindowHandle *tryGetWindowFromSurface(const wl_surface *surface) {
    if (const auto result = SURFACE_TO_HANDLES.find(surface); result != SURFACE_TO_HANDLES.end()) {
        return result->second;
    }
    return nullptr;
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
    },
    .enter = [](void *data,
                struct wl_keyboard *wl_keyboard,
                uint32_t serial,
                struct wl_surface *surface,
                struct wl_array *keys) {
    },
    .leave = [](void *data,
                struct wl_keyboard *wl_keyboard,
                uint32_t serial,
                struct wl_surface *surface) {
    },
    .key = [](void *data,
              struct wl_keyboard *wl_keyboard,
              uint32_t serial,
              uint32_t time,
              uint32_t key,
              uint32_t state) {
    },
    .modifiers = [](void *data,
                    struct wl_keyboard *wl_keyboard,
                    uint32_t serial,
                    uint32_t mods_depressed,
                    uint32_t mods_latched,
                    uint32_t mods_locked,
                    uint32_t group) {
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
            FOCUSED_HANDLE = handle;
            WindowEvent ev{};
            new(&ev.enter) CursorEnterEvent{
                .type = WindowEventType::CursorEnter,
                .handle = handle,
            };
            PENDING_EVENTS.push_back(ev);
        }
    },
    .leave = [](void *data,
                struct wl_pointer *wl_pointer,
                uint32_t serial,
                struct wl_surface *surface) {
        if (const auto handle = tryGetWindowFromSurface(surface)) {
            FOCUSED_HANDLE = nullptr;
            WindowEvent ev{};
            new(&ev.enter) CursorLeaveEvent{
                .type = WindowEventType::CursorLeave,
                .handle = handle,
            };
            PENDING_EVENTS.push_back(ev);
        }
    },
    .motion = [](void *data,
                 struct wl_pointer *wl_pointer,
                 uint32_t time,
                 wl_fixed_t surface_x,
                 wl_fixed_t surface_y) {
        if (const auto handle = FOCUSED_HANDLE) {
            const auto x = static_cast<float>(wl_fixed_to_double(surface_x));
            const auto y = static_cast<float>(wl_fixed_to_double(surface_y));
            WindowEvent ev{};
            handle->cursorPosition = {x, y};

            new(&ev.enter) CursorMoveEvent{
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
        if (const auto handle = FOCUSED_HANDLE) {
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
            new(&ev.enter) CursorButtonEvent{
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

EXPORT_IMPL void platformInit() {
    DISPLAY = wl_display_connect(nullptr);
    //wl_display_add_listener(DISPLAY,&displayListener,nullptr); // errors out with display already has a listener ?
    REGISTRY = wl_display_get_registry(DISPLAY);
    wl_registry_add_listener(REGISTRY, &registryListener, nullptr);
    wl_display_roundtrip(DISPLAY);
    DECOR_CONTEXT = libdecor_new(DISPLAY, &decorInterface);
}

EXPORT_IMPL void platformShutdown() {
    if (KEYBOARD) wl_keyboard_destroy(KEYBOARD);
    if (POINTER) wl_pointer_destroy(POINTER);
    if (SEAT) wl_seat_destroy(SEAT);
    if (DECOR_CONTEXT) libdecor_unref(DECOR_CONTEXT);
    if (COMPOSITOR) wl_compositor_destroy(COMPOSITOR);
    if (REGISTRY) wl_registry_destroy(REGISTRY);
    if (DISPLAY) wl_display_disconnect(DISPLAY);
}

EXPORT_IMPL int platformGet() {
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

EXPORT_IMPL void platformSelectFile(const char *title, bool multiple, const char *filter,
                                    PathReceivedCallback callback) {
    std::ostringstream cmd;
    cmd << "zenity --file-selection";
    if (multiple)
        cmd << " --multiple --separator=\":\"";
    if (title)
        cmd << " --title=\"" << title << "\"";

    runZenityCommand(cmd, multiple,filter, callback);
}

EXPORT_IMPL void platformSelectPath(const char *title, bool multiple, PathReceivedCallback callback) {
    std::ostringstream cmd;
    cmd << "zenity --file-selection --directory";
    if (multiple)
        cmd << " --multiple --separator=\":\"";
    if (title)
        cmd << " --title=\"" << title << "\"";

    runZenityCommand(cmd, multiple,nullptr, callback);
}

EXPORT_IMPL void platformWindowPump() {
    wl_display_roundtrip(DISPLAY);
}

EXPORT_IMPL void *platformWindowCreate(const char *title, int width, int height, Flags<WindowFlags> flags) {
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

EXPORT_IMPL void platformWindowDestroy(void *handle) {
    if (handle == FOCUSED_HANDLE) {
        FOCUSED_HANDLE = nullptr;
    }
    auto windowHandle = static_cast<WindowHandle *>(handle);
    SURFACE_TO_HANDLES.erase(windowHandle->surface);
    libdecor_frame_unref(windowHandle->frame);
    wl_surface_destroy(windowHandle->surface);
    delete windowHandle;
}

EXPORT_IMPL void platformWindowShow(void *handle) {
}

EXPORT_IMPL void platformWindowHide(void *handle) {
}

EXPORT_IMPL Vector2 platformWindowGetCursorPosition(void *handle) {
    auto windowHandle = static_cast<WindowHandle *>(handle);
    return windowHandle->cursorPosition;
}

EXPORT_IMPL void platformWindowSetCursorPosition(void *handle, Vector2 position) {
}

EXPORT_IMPL Extent2D platformWindowGetSize(void *handle) {
    auto windowHandle = static_cast<WindowHandle *>(handle);
    return windowHandle->size;
}

EXPORT_IMPL VkSurfaceKHR platformWindowCreateSurface(VkInstance instance, void *handle) {
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

EXPORT_IMPL int platformWindowGetEvents(WindowEvent *output, int size) {
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

EXPORT_IMPL void platformWindowStartTyping(void *handle) {
}

EXPORT_IMPL void platformWindowStopTyping(void *handle) {
}

EXPORT_IMPL void platformWindowSetSize(void *handle, Extent2D size) {
}
#endif


#ifdef RIN_PLATFORM_MAC

EXPORT_IMPL void platformInit()
{

}
EXPORT_IMPL int platformGet()
{
    return static_cast<int>(EPlatform::Mac);
}

EXPORT_IMPL void platformInit()
{

}
EXPORT_IMPL int platformGet()
{
    return static_cast<int>(EPlatform::Linux);
}
EXPORT_IMPL void platformSelectFile(const char* title, bool multiple, const char* filter, PathReceivedCallback callback)
{

}
EXPORT_IMPL void platformSelectPath(const char* title, bool multiple, PathReceivedCallback callback)
{
    
}
#endif
