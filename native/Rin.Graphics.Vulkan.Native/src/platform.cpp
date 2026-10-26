#include "platform.hpp"
#include "rwin/rwin.h"
#include "rwin/DropCallbacks.h"
#include <iostream>
#include <string>
#include <vector>

namespace
{
    // Flattens what rwin offers into pointers the managed side can read during the callback.
    struct DropDataBuffer
    {
        std::u16string text;
        std::vector<std::string> files;
        std::vector<const char*> filePointers;
        PlatformDropData data{};

        explicit DropDataBuffer(const rwin::IDataView& view)
        {
            if (view.HasText() && view.GetText(text))
            {
                data.text = text.c_str();
                data.textLength = text.size();
            }

            std::vector<std::filesystem::path> paths;
            if (view.HasFiles() && view.GetFiles(paths))
            {
                files.reserve(paths.size());
                for (const auto& path : paths)
                {
                    const auto utf8 = path.u8string();
                    files.emplace_back(utf8.begin(), utf8.end());
                }
                filePointers.reserve(files.size());
                for (const auto& file : files) filePointers.push_back(file.c_str());
                data.files = filePointers.data();
                data.fileCount = filePointers.size();
            }
        }
    };

    PlatformDropInfo ToPlatformDropInfo(const rwin::DropInfo& info)
    {
        return PlatformDropInfo{
            .position = info.position,
            .allowed = static_cast<std::uint32_t>(info.allowed),
            .modifiers = static_cast<std::uint32_t>(info.modifiers),
        };
    }

    std::function<rwin::DropEffect(const rwin::DropInfo&, const rwin::IDataView&)> WrapDropCallback(
        const PlatformDropCallback callback, void* userData)
    {
        if (!callback) return {};
        return [callback, userData](const rwin::DropInfo& info, const rwin::IDataView& view)
        {
            const DropDataBuffer buffer{view};
            const auto platformInfo = ToPlatformDropInfo(info);
            return static_cast<rwin::DropEffect>(callback(&platformInfo, &buffer.data, userData));
        };
    }
}

void platformWindowPump()
{
    rwin::pumpEvents();
}

GraphicsResult platformWindowCreate(const char* title, rwin::Extent2D extent, rwin::Flags<rwin::WindowFlags> flags, std::uint64_t* outHandle)
{
    return guarded([&]
    {
        require(title != nullptr && outHandle != nullptr, GraphicsResult::InvalidArgument);
        const auto handle = rwin::createWindow(std::string_view{title}, extent, flags);
        require(handle != 0, GraphicsResult::WindowCreationFailed);
        *outHandle = handle;
    });
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

GraphicsResult platformWindowCreateSurface(VkInstance instance, std::uint64_t handle, VkSurfaceKHR* outSurface)
{
    return guarded([&]
    {
        require(instance != VK_NULL_HANDLE && outSurface != nullptr, GraphicsResult::InvalidArgument);
        const auto surface = rwin::createSurface(handle, instance);
        require(surface != VK_NULL_HANDLE, GraphicsResult::SurfaceCreationFailed);
        *outSurface = surface;
    });
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

void platformWindowSetDropCallbacks(std::uint64_t handle, const PlatformDropCallbacks* callbacks)
{
    rwin::DropCallbacks wrapped{
        .enter = WrapDropCallback(callbacks->enter, callbacks->userData),
        .over = WrapDropCallback(callbacks->over, callbacks->userData),
        .drop = WrapDropCallback(callbacks->drop, callbacks->userData),
    };
    if (callbacks->leave)
    {
        const auto leave = callbacks->leave;
        const auto userData = callbacks->userData;
        wrapped.leave = [leave, userData] { leave(userData); };
    }
    rwin::setWindowDropCallbacks(handle, wrapped);
}

void platformWindowClearDropCallbacks(std::uint64_t handle)
{
    rwin::clearWindowDropCallbacks(handle);
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
