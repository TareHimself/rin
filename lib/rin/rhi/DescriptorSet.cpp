#include "rin/rhi/DescriptorSet.h"

namespace rin::rhi
{

    void DescriptorSet::ResourceArray::OnDispose()
    {
        
    }
    DescriptorSet::DescriptorSet(const vk::DescriptorSet& set)
    {
        _set = set;
    }
    vk::DescriptorSet DescriptorSet::GetDescriptorSet() const
    {
        return _set;
    }
}
