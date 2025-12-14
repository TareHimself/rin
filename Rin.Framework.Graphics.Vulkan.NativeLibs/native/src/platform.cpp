#include "platform.hpp"
#include "rwin/rwin.h"
#include <iostream>

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
