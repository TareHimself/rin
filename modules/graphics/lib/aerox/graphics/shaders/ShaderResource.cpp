#pragma once
#include "aerox/graphics/shaders/ShaderResource.hpp"
namespace aerox::graphics
{
    ShaderResource::ShaderResource(const std::string& inName, uint32_t inSet, uint32_t inBinding, uint32_t inCount,
        vk::DescriptorType inType, const vk::ShaderStageFlags& inStages,
        const vk::DescriptorBindingFlags& inBindingFlags, uint32_t inSize)
    {
        name = inName;
        set = inSet;
        binding = inBinding;
        count = inCount;
        type = inType;
        stages = inStages;
        bindingFlags = inBindingFlags;
        size = inSize;
    }
}
