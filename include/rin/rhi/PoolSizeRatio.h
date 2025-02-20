#pragma once
#include "DescriptorLayoutBuilder.h"
namespace rin::rhi
{
    struct PoolSizeRatio
    {
        vk::DescriptorType type;
        float ratio;
    };
}
