#include "aerox/graphics/SamplerSpec.hpp"

namespace aerox::graphics
{
    SamplerSpec::SamplerSpec(const vk::Filter& filter, const vk::ImageTiling& tiling)
    {
        _filter = filter;
        _tiling = tiling;
    }

    std::string SamplerSpec::GetId() const
    {
        return std::to_string(static_cast<int>(_filter)) + "_" + std::to_string(static_cast<int>(_tiling));
    }
}
