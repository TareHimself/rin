#pragma once
#include <cstdint>

namespace rin::rhi
{
    struct IGraphDescriptor
    {
        virtual ~IGraphDescriptor() = default;
        virtual uint64_t ComputeHashCode() = 0;
    };

}
