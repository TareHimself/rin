#pragma once
#include "shaders/Shader.hpp"

namespace aerox::graphics
{
    struct BoundTexture
    {
        Shared<DeviceImage> image{};
        vk::Filter filter{};
        vk::ImageTiling tiling{};
        bool isMipMapped = false;
        std::string debugName{};
        bool valid;
        BoundTexture(const Shared<DeviceImage>& inImage,vk::Filter inFilter,vk::ImageTiling inTiling,bool inIsMipMapped,const std::string& inDebugName);
    };
}
