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

