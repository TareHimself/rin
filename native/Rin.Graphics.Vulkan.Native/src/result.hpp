#pragma once
#include <cstdint>
#include <exception>
#include <new>
#include <vulkan/vulkan_core.h>

// Mirrored by Native.GraphicsResult in Engine/Rin.Graphics.Vulkan/Native.cs. Keep the two in sync.
enum class GraphicsResult : std::int32_t
{
    Ok = 0,
    InvalidArgument,
    InstanceCreationFailed,
    NoSuitableDevice,
    DeviceCreationFailed,
    FunctionLoadingFailed,
    WindowCreationFailed,
    SurfaceCreationFailed,
    OutOfHostMemory,
    OutOfDeviceMemory,
    DeviceLost,
    VulkanError,
    PlatformError,
};

struct ResultError final : std::exception
{
    GraphicsResult result;

    explicit ResultError(const GraphicsResult inResult) : result(inResult)
    {
    }
};

inline GraphicsResult toGraphicsResult(const VkResult result)
{
    switch (result)
    {
    case VK_SUCCESS:
        return GraphicsResult::Ok;
    case VK_ERROR_OUT_OF_HOST_MEMORY:
        return GraphicsResult::OutOfHostMemory;
    case VK_ERROR_OUT_OF_DEVICE_MEMORY:
        return GraphicsResult::OutOfDeviceMemory;
    case VK_ERROR_DEVICE_LOST:
        return GraphicsResult::DeviceLost;
    default:
        return result < 0 ? GraphicsResult::VulkanError : GraphicsResult::Ok;
    }
}

inline void check(const VkResult result)
{
    if (result < 0) throw ResultError(toGraphicsResult(result));
}

inline void require(const bool condition, const GraphicsResult onFailure)
{
    if (!condition) throw ResultError(onFailure);
}

// Every fallible export runs its body through this so no exception reaches the managed caller.
template <typename F>
GraphicsResult guarded(F&& body) noexcept
{
    try
    {
        body();
        return GraphicsResult::Ok;
    }
    catch (const ResultError& e)
    {
        return e.result;
    }
    catch (const std::bad_alloc&)
    {
        return GraphicsResult::OutOfHostMemory;
    }
    catch (...)
    {
        return GraphicsResult::PlatformError;
    }
}
