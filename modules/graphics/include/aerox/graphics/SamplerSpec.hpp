#pragma once
#include "vulkan/vulkan.hpp"

namespace aerox::graphics
{
    struct SamplerSpec
    {
    private:
        vk::Filter _filter{};
        vk::ImageTiling _tiling{};
    public:
        SamplerSpec(const vk::Filter& filter,const vk::ImageTiling& tiling);

        std::string GetId() const;
    };
}
