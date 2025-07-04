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

RIN_NATIVE_API int platformGet();

RIN_NATIVE_API void platformInit();

RIN_NATIVE_API void platformShutdown();

using PathReceivedCallback = void(RIN_CALLBACK_CONVENTION *)(const char* text);

RIN_NATIVE_API void platformSelectFile(const char * title,bool multiple, const char * filter,PathReceivedCallback callback);

RIN_NATIVE_API void platformSelectPath(const char * title,bool multiple,PathReceivedCallback callback);

RIN_NATIVE_API void platformWindowPump();

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

RIN_NATIVE_API void* platformWindowCreate(const char * title,int width,int height,Flags<WindowFlags> flags);

RIN_NATIVE_API void platformWindowDestroy(void * handle);

RIN_NATIVE_API void platformWindowShow(void * handle);

RIN_NATIVE_API void platformWindowHide(void * handle);

RIN_NATIVE_API Vector2 platformWindowGetCursorPosition(void * handle);

RIN_NATIVE_API void platformWindowSetCursorPosition(void * handle,Vector2 position);

RIN_NATIVE_API Extent2D platformWindowGetSize(void *handle);

RIN_NATIVE_API VkSurfaceKHR platformWindowCreateSurface(VkInstance instance,void * handle);

RIN_NATIVE_API int platformWindowGetEvents(WindowEvent * output,int size);

RIN_NATIVE_API void platformWindowStartTyping(void* handle);

RIN_NATIVE_API void platformWindowStopTyping(void* handle);

RIN_NATIVE_API void platformWindowSetSize(void* handle,Extent2D size);

RIN_NATIVE_API void platformWindowSetPosition(void* handle,Point2D position);
