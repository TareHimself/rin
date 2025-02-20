#pragma once
#include <cstdint>
#include <vulkan/vulkan.hpp>
#include "rin/core/macros.h"

namespace rin::rhi
{
    enum class ImageTiling : uint8_t
    {
        Repeat,
    ClampEdge,
    ClampBorder
    };

    inline vk::SamplerAddressMode imageTilingToVulkanAddressMode(const ImageTiling& tiling)
    {
        switch(tiling)
        {
        case ImageTiling::Repeat:
            return vk::SamplerAddressMode::eRepeat;
        case ImageTiling::ClampEdge:
            return vk::SamplerAddressMode::eClampToEdge;
        case ImageTiling::ClampBorder:
            return vk::SamplerAddressMode::eClampToBorder;
        default:
            NOT_IMPLEMENTED
        }
    }

    inline ImageTiling vulkanAddressModeToImageTiling(const vk::SamplerAddressMode& addressMode)
    {
        switch(addressMode)
        {
        case vk::SamplerAddressMode::eRepeat:
            return ImageTiling::Repeat;
        case vk::SamplerAddressMode::eClampToEdge:
            return ImageTiling::ClampEdge;
        case vk::SamplerAddressMode::eClampToBorder:
            return ImageTiling::ClampBorder;
        default:
            NOT_IMPLEMENTED
        }
    }
}
