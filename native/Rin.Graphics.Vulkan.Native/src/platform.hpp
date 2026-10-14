#pragma once
#include <rwin/types.h>
#include <rwin/flags.h>
#include <vulkan/vulkan_core.h>
#include "flags.hpp"
#include "macro.hpp"

RIN_NATIVE_API void platformWindowPump();

RIN_NATIVE_API std::uint64_t platformWindowCreate(const char * title,rwin::Extent2D extent,rwin::Flags<rwin::WindowFlags> flags);

RIN_NATIVE_API void platformWindowDestroy(std::uint64_t handle);

RIN_NATIVE_API void platformWindowShow(std::uint64_t handle);

RIN_NATIVE_API void platformWindowHide(std::uint64_t handle);

RIN_NATIVE_API rwin::Vector2 platformWindowGetCursorPosition(std::uint64_t handle);

RIN_NATIVE_API void platformWindowSetCursorPosition(std::uint64_t handle,rwin::Vector2 position);

RIN_NATIVE_API rwin::Extent2D platformWindowGetSize(std::uint64_t handle);

RIN_NATIVE_API VkSurfaceKHR platformWindowCreateSurface(VkInstance instance,std::uint64_t handle);

RIN_NATIVE_API int platformWindowGetEvents(rwin::WindowEvent * output,int size);

RIN_NATIVE_API void platformWindowStartTextInput(std::uint64_t handle,rwin::Rect2D caret);

RIN_NATIVE_API void platformWindowStopTextInput(std::uint64_t handle);

RIN_NATIVE_API void platformWindowSetTextInputRect(std::uint64_t handle,rwin::Rect2D caret);

// The UTF-16 text of a text event, valid until the next platformWindowPump, or nullptr if it is gone.
RIN_NATIVE_API const char16_t * platformWindowGetEventText(rwin::TextRef text);

// What another app is dragging. Text is UTF-16, files are UTF-8 paths. Only valid inside the callback.
struct PlatformDropData
{
    const char16_t* text;
    std::uint64_t textLength;
    const char* const* files;
    std::uint64_t fileCount;
};

// Position is window local in pixels. allowed and modifiers are rwin::DropEffect and rwin::InputModifier flags.
struct PlatformDropInfo
{
    rwin::Vector2 position;
    std::uint32_t allowed;
    std::uint32_t modifiers;
};

// Returns the rwin::DropEffect to apply, None to refuse.
using PlatformDropCallback = std::uint32_t(RIN_CALLBACK_CONVENTION*)(const PlatformDropInfo* info, const PlatformDropData* data, void* userData);
using PlatformDropLeaveCallback = void(RIN_CALLBACK_CONVENTION*)(void* userData);

struct PlatformDropCallbacks
{
    PlatformDropCallback enter;
    PlatformDropCallback over;
    PlatformDropCallback drop;
    PlatformDropLeaveCallback leave;
    void* userData;
};

// A window refuses drops until this is called. Any callback may be null, see rwin::DropCallbacks for the defaults.
RIN_NATIVE_API void platformWindowSetDropCallbacks(std::uint64_t handle, const PlatformDropCallbacks* callbacks);

RIN_NATIVE_API void platformWindowClearDropCallbacks(std::uint64_t handle);

RIN_NATIVE_API int platformHasClipboardText();

// The clipboard's text as UTF-16, valid until the next clipboard call, or nullptr when it holds no text.
RIN_NATIVE_API const char16_t * platformGetClipboardText();

RIN_NATIVE_API int platformSetClipboardText(const char16_t * text,std::uint64_t length);

RIN_NATIVE_API void platformClearClipboard();
