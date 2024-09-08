#include "aerox/graphics/descriptors/PoolSizeRatio.hpp"
namespace aerox::graphics
{
    PoolSizeRatio::PoolSizeRatio(vk::DescriptorType inType, float inRatio)
    {
        type = inType;
        ratio = inRatio;
    }
}