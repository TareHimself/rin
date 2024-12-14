#pragma once
#include <cstdint>

namespace rin::graphics
{
    struct IGraphDescriptor
    {
        virtual ~IGraphDescriptor() = default;
        virtual uint64_t ComputeHashCode() = 0;
    };

}
