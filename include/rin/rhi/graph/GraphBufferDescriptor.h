#pragma once
#include "IGraphDescriptor.h"

namespace rin::rhi
{
    struct GraphBufferDescriptor : IGraphDescriptor
    {
        uint64_t size;
        
        uint64_t ComputeHashCode() override;

    };
    inline uint64_t GraphBufferDescriptor::ComputeHashCode()
    {
        return hashCombine(size);
    }
}
