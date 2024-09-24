#pragma once
#include "vulkan/vulkan.hpp"
struct PoolSizeRatio
{
    vk::DescriptorType type;
    float ratio;

    PoolSizeRatio(vk::DescriptorType inType,float inRatio);
};
