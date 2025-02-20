#pragma once
#include <cstdint>
#include <vulkan/vulkan.hpp>
#include "rin/core/macros.h"

namespace rin::rhi
{
    enum class ImageFilter : uint8_t
    {
        Linear,
        Nearest,
        Cubic
    };

    inline vk::Filter imageFilterToVulkanFilter(const ImageFilter& filter)
    {
        switch(filter)
        {
        case ImageFilter::Linear:
            return vk::Filter::eLinear;
        case ImageFilter::Nearest:
            return vk::Filter::eNearest;
        case ImageFilter::Cubic:
            return vk::Filter::eCubicIMG;
        default:
            NOT_IMPLEMENTED
        }
    }

    inline ImageFilter vulkanFilterToImageFilter(const vk::Filter& filter)
    {
        switch(filter)
        {
        case vk::Filter::eLinear:
            return ImageFilter::Linear;
        case vk::Filter::eNearest:
            return ImageFilter::Nearest;
        case vk::Filter::eCubicIMG:
            return ImageFilter::Cubic;
        default:
            NOT_IMPLEMENTED
        }
    }

}
