#include "aerox/graphics/descriptors/PoolSizeRatio.hpp"
PoolSizeRatio::PoolSizeRatio(vk::DescriptorType inType, float inRatio)
{
    type = inType;
    ratio = inRatio;
}