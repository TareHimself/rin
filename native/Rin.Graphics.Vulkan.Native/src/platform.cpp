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

void platformWindowStartTextInput(std::uint64_t handle, rwin::Rect2D caret)
{
    rwin::startTextInput(handle, caret);
}

void platformWindowStopTextInput(std::uint64_t handle)
{
    rwin::stopTextInput(handle);
}

void platformWindowSetTextInputRect(std::uint64_t handle, rwin::Rect2D caret)
{
    rwin::setTextInputRect(handle, caret);
}

const char16_t* platformWindowGetEventText(rwin::TextRef text)
{
    const auto view = rwin::getEventText(text);
    return view.size() == text.length ? view.data() : nullptr;
}

int platformHasClipboardText()
{
    return rwin::hasClipboardText();
}

const char16_t* platformGetClipboardText()
{
    static std::u16string text;
    text.clear();
    return rwin::getClipboardText(text) ? text.c_str() : nullptr;
}

int platformSetClipboardText(const char16_t* text, const std::uint64_t length)
{
    return rwin::setClipboardText(std::u16string_view(text, length));
}

void platformClearClipboard()
{
    rwin::clearClipboard();
}
