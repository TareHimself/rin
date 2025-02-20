#pragma once
#define VULKAN_HPP_FLAGS_MASK_TYPE_AS_PUBLIC
#include "IGraphDescriptor.h"
#include <vulkan/vulkan.hpp>

#include "rin/core/utils.h"
#include "rin/rhi/ImageFormat.h"

namespace rin::rhi
{
    struct GraphImageDescriptor : IGraphDescriptor
    {
        uint64_t ComputeHashCode() override;
        vk::Extent3D extent{};
        ImageFormat format{};
        vk::ImageLayout layout{};
        vk::ImageUsageFlags usage{};
        bool mips{false};
    private:
        std::optional<uint64_t> _hashCode = 0;
    };

    inline uint64_t GraphImageDescriptor::ComputeHashCode()
    {
        if(_hashCode.has_value())
        {
            return *_hashCode;
        }
        _hashCode = hashCombine(extent.width,extent.height,extent.depth,static_cast<int>(format), static_cast<int>(layout),static_cast<uint32_t>(usage),static_cast<int>(mips));
        return *_hashCode;
    }
}
