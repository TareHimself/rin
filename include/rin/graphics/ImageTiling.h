#pragma once
#include <cstdint>
#include <vulkan/vulkan.hpp>
#include "rin/core/macros.h"

namespace rin::graphics
{
    enum class ImageTiling : uint8_t
    {
        Repeat,
    ClampEdge,
    ClampBorder
    };

    // inline vk::Format imageFormatToVulkanFormat(const ImageFormat& format)
    // {
    //     switch (format)
    //     {
    //     case ImageFormat::RGBA8:
    //         return vk::Format::eR8G8B8A8Unorm;
    //     case ImageFormat::RGBA32:
    //         return vk::Format::eR32G32B32A32Sfloat;
    //     case ImageFormat::Depth:
    //         return vk::Format::eD32Sfloat;
    //     case ImageFormat::Stencil:
    //         return vk::Format::eD32SfloatS8Uint;
    //     default:
    //         NOT_IMPLEMENTED
    //     }
    // }
    //
    // inline ImageFormat vulkanFormatToImageFormat(const vk::Format& format)
    // {
    //     switch (format)
    //     {
    //     case vk::Format::eR8G8B8A8Unorm:
    //         return ImageFormat::RGBA8;
    //     case vk::Format::eR32G32B32A32Sfloat:
    //         return ImageFormat::RGBA32;
    //     case vk::Format::eD32Sfloat:
    //         return ImageFormat::Depth;
    //     case vk::Format::eD32SfloatS8Uint:
    //         return ImageFormat::Stencil;
    //     default:
    //         NOT_IMPLEMENTED
    //     }
    // }
}
