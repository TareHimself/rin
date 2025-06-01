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
#include <vulkan/vulkan.h>
#include <vulkan/vulkan_wayland.h>
#include <list>
#include <wayland-client.h>
#include <xdg-shell-client-protocol.h>
#include <libdecor.h>

wl_registry * REGISTRY;
wl_compositor * COMPOSITOR;
wl_display * DISPLAY;
xdg_wm_base * SHELL;

static std::list<WindowEvent> PENDING_EVENTS = {};

static void handleRegistry(
    void* data,
    struct wl_registry* registry,
    uint32_t name,
    const char* interface,
    uint32_t version
);
static void handleShellPing(void* data, struct xdg_wm_base* shell, uint32_t serial);
static void handleShellSurfaceConfigure(void* data, struct xdg_surface* shellSurface, uint32_t serial);
static void handleToplevelConfigure(
    void* data,
    struct xdg_toplevel* toplevel,
    int32_t width,
    int32_t height,
    struct wl_array* states
);
static void handleToplevelClose(void* data, struct xdg_toplevel* toplevel);
static void handleTopLevelDecorationConfigure(void *data,
              struct zxdg_toplevel_decoration_v1 *zxdg_toplevel_decoration_v1,
              uint32_t mode);

static const struct wl_registry_listener registryListener = {
    .global = handleRegistry
};

static const struct xdg_wm_base_listener shellListener = {
    .ping = handleShellPing
};

static const struct xdg_surface_listener shellSurfaceListener = {
    .configure = handleShellSurfaceConfigure
};

static const struct xdg_toplevel_listener topLevelListener = {
    .configure = handleToplevelConfigure,
    .close = handleToplevelClose
};

struct WindowHandle {
    wl_surface * surface;
    xdg_surface * xdgSurface;
    xdg_toplevel * xdgToplevel;
    Flags<WindowFlags> flags;
    Extent2D size;
    zxdg_toplevel_decoration_v1 * decoration;
    bool resizePending = false;
};

static void handleRegistry(
    void* data,
    struct wl_registry* registry,
    uint32_t name,
    const char* interface,
    uint32_t version
)
{
    auto interfaceName = std::string{interface};

    if (interfaceName == wl_compositor_interface.name)
    {
        COMPOSITOR = static_cast<wl_compositor *>(wl_registry_bind(registry, name, &wl_compositor_interface, 1));
    }
    else if (interfaceName == xdg_wm_base_interface.name)
    {
        SHELL = static_cast<xdg_wm_base*>(wl_registry_bind(registry, name, &xdg_wm_base_interface, 1));
        xdg_wm_base_add_listener(SHELL, &shellListener, nullptr);
    }
}

static void handleShellPing(void* data, struct xdg_wm_base* shell, uint32_t serial)
{
    xdg_wm_base_pong(shell, serial);
}

static void handleShellSurfaceConfigure(void* data, struct xdg_surface* shellSurface, uint32_t serial)
{
    auto handle = static_cast<WindowHandle*>(data);

    xdg_surface_ack_configure(shellSurface, serial);

    if (handle->resizePending) {
        handle->resizePending = false;

        WindowEvent ev{};
        new(&ev.resize) ResizeEvent{
            .type = WindowEventType::Resize,
            .handle = handle,
            .size = handle->size,

        };
        PENDING_EVENTS.push_back(ev);
    }
}

static void handleToplevelConfigure(
    void* data,
    struct xdg_toplevel* toplevel,
    int32_t width,
    int32_t height,
    struct wl_array* states
)
{
    auto handle = static_cast<WindowHandle*>(data);
    if (width == 0 || height == 0) {
        // Compositor is asking us to size ourself
        return;
    }
    auto newExtent = Extent2D{static_cast<uint32_t>(width),static_cast<uint32_t>(height)};
    if (newExtent != handle->size) {
        handle->size = newExtent;
        handle->resizePending = true;
    }
}

static void handleToplevelClose(void* data, struct xdg_toplevel* toplevel)
{
    auto handle = static_cast<WindowHandle*>(data);
    WindowEvent ev{};
    new(&ev.close) CloseEvent{
        .type = WindowEventType::Close,
        .handle = handle,
    };
    PENDING_EVENTS.push_back(ev);
}

static void handleTopLevelDecorationConfigure(void *data,
              struct zxdg_toplevel_decoration_v1 *zxdg_toplevel_decoration_v1,
              uint32_t mode) {
    auto handle = static_cast<WindowHandle*>(data);


}
EXPORT_IMPL void platformInit()
{
    DISPLAY = wl_display_connect(nullptr);
    REGISTRY = wl_display_get_registry(DISPLAY);
    wl_registry_add_listener(REGISTRY, &registryListener,nullptr);
    wl_display_roundtrip(DISPLAY);
}

EXPORT_IMPL void platformShutdown() {
    xdg_wm_base_destroy(SHELL);
    wl_compositor_destroy(COMPOSITOR);
    wl_registry_destroy(REGISTRY);
    wl_display_disconnect(DISPLAY);
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

EXPORT_IMPL void platformWindowPump() {

    wl_display_roundtrip(DISPLAY);
}

EXPORT_IMPL void * platformWindowCreate(const char *title, int width, int height, Flags<WindowFlags> flags) {

    auto surface = wl_compositor_create_surface(COMPOSITOR);
    auto shellSurface = xdg_wm_base_get_xdg_surface(SHELL, surface);
    auto toplevel = xdg_surface_get_toplevel(shellSurface);
    xdg_toplevel_set_title(toplevel,title);
    xdg_toplevel_set_app_id(toplevel,"Test APP");
    auto windowHandle = new WindowHandle{
        .surface = surface,
        .xdgSurface = shellSurface,
        .xdgToplevel = toplevel,
        .flags = flags,
        .size = {static_cast<uint32_t>(width),static_cast<uint32_t>(height)},
    };
    xdg_surface_add_listener(shellSurface, &shellSurfaceListener, windowHandle);
    xdg_toplevel_add_listener(toplevel, &topLevelListener, windowHandle);
    wl_surface_commit(surface);
    wl_display_roundtrip(DISPLAY);
    wl_surface_commit(surface);
    return windowHandle;
}

EXPORT_IMPL void platformWindowDestroy(void *handle) {
    auto windowHandle = static_cast<WindowHandle*>(handle);
    xdg_toplevel_destroy(windowHandle->xdgToplevel);
    xdg_surface_destroy(windowHandle->xdgSurface);
    wl_surface_destroy(windowHandle->surface);
    delete windowHandle;
}

EXPORT_IMPL void platformWindowShow(void *handle) {

}

EXPORT_IMPL void platformWindowHide(void *handle) {
}

EXPORT_IMPL Vector2 platformWindowGetCursorPosition(void *handle) {
    return Vector2{};
}

EXPORT_IMPL void platformWindowSetCursorPosition(void *handle, Vector2 position) {
}

EXPORT_IMPL Extent2D platformWindowGetSize(void *handle) {
    auto windowHandle = static_cast<WindowHandle*>(handle);
    return windowHandle->size;
}

EXPORT_IMPL VkSurfaceKHR platformWindowCreateSurface(VkInstance instance, void *handle) {
    auto windowHandle = static_cast<WindowHandle*>(handle);

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
