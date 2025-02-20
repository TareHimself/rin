#pragma once
#include "DescriptorLayoutBuilder.h"
#include "ImageFilter.h"
#include "ImageTiling.h"
namespace rin::rhi
{
    struct SamplerInfo
    {
        ImageTiling tiling{};
        ImageFilter filter{};
    };
}
