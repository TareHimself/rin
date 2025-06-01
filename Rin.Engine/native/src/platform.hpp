#pragma once
#include <vulkan/vulkan_core.h>
#include "flags.hpp"
#include "macro.hpp"
#include "structs.hpp"

enum class EPlatform
{
    Windows,
    Linux,
    Mac
};

EXPORT int platformGet();

EXPORT void platformInit();

EXPORT void platformShutdown();

using PathReceivedCallback = void(RIN_CALLBACK_CONVENTION *)(const char* text);

EXPORT void platformSelectFile(const char * title,bool multiple, const char * filter,PathReceivedCallback callback);

EXPORT void platformSelectPath(const char * title,bool multiple,PathReceivedCallback callback);

EXPORT void platformWindowPump();

enum class WindowFlags : uint32_t
{
    None = 0,
    Frameless =  1 << 0,
    Floating =  1 << 1,
    Resizable =  1 << 2,
    Visible =  1 << 3,
    Transparent =  1 << 4,
    Focused =  1 << 5,
};

EXPORT void* platformWindowCreate(const char * title,int width,int height,Flags<WindowFlags> flags);

EXPORT void platformWindowDestroy(void * handle);

EXPORT void platformWindowShow(void * handle);

EXPORT void platformWindowHide(void * handle);

EXPORT Vector2 platformWindowGetCursorPosition(void * handle);

EXPORT void platformWindowSetCursorPosition(void * handle,Vector2 position);

EXPORT Extent2D platformWindowGetSize(void *handle);

EXPORT VkSurfaceKHR platformWindowCreateSurface(VkInstance instance,void * handle);

EXPORT int platformWindowGetEvents(WindowEvent * output,int size);

EXPORT void platformWindowStartTyping(void* handle);

EXPORT void platformWindowStopTyping(void* handle);

EXPORT void platformWindowSetSize(void* handle,Extent2D size);

EXPORT void platformWindowSetPosition(void* handle,Point2D position);
