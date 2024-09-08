#pragma once
#include "vulkan/vulkan.hpp"
namespace aerox::graphics
{
    struct PoolSizeRatio
    {
        vk::DescriptorType type;
        float ratio;

        PoolSizeRatio(vk::DescriptorType inType,float inRatio);
    };
}
