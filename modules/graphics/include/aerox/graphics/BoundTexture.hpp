#pragma once
#include "shaders/Shader.hpp"

struct BoundTexture
{
    Shared<DeviceImage> image{};
    vk::Filter filter{};
    vk::SamplerAddressMode tiling{};
    bool isMipMapped = false;
    std::string debugName{};
    bool valid;
    BoundTexture();
    BoundTexture(const Shared<DeviceImage>& inImage,vk::Filter inFilter,vk::SamplerAddressMode inTiling,bool inIsMipMapped,const std::string& inDebugName);
};
