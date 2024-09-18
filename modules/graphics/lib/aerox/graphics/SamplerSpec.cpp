#include "aerox/graphics/SamplerSpec.hpp"

namespace aerox::graphics
{
    SamplerSpec::SamplerSpec(const vk::Filter& inFilter, const vk::SamplerAddressMode& inTiling)
    {
        filter = inFilter;
        tiling = inTiling;
    }

    std::string SamplerSpec::GetId() const
    {
        return std::to_string(static_cast<int>(filter)) + "_" + std::to_string(static_cast<int>(tiling));
    }
}
