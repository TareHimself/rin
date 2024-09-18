#include "aerox/graphics/BoundTexture.hpp"
namespace aerox::graphics
{
    BoundTexture::BoundTexture()
    {
        valid = false;
    }

    BoundTexture::BoundTexture(const Shared<DeviceImage>& inImage, vk::Filter inFilter,
        vk::SamplerAddressMode inTiling, bool inIsMipMapped, const std::string& inDebugName)
    {
        image = inImage;
        filter = inFilter;
        tiling = inTiling;
        isMipMapped = inIsMipMapped;
        debugName = inDebugName;
        valid = true;
    }
}
