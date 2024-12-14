#pragma once
#include "IGraphDescriptor.h"
#include <vulkan/vulkan.hpp>

#include "rin/graphics/ImageFormat.h"

namespace rin::graphics
{
    struct GraphImageDescriptor : IGraphDescriptor
    {
        uint64_t ComputeHashCode() override;
        vk::Extent3D extent{};
        ImageFormat format{};
        vk::ImageLayout layout{};
    };

    inline uint64_t GraphImageDescriptor::ComputeHashCode()
    {
        
    }
}
