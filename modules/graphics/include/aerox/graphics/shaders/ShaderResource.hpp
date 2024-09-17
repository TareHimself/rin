#pragma once
#include <string>
#include <vulkan/vulkan.hpp>
namespace aerox::graphics
{
    struct ShaderResource
    {
        std::string name;
        uint32_t set;
        uint32_t binding;
        uint32_t count;
        vk::DescriptorType type;
        vk::ShaderStageFlags stages;
        vk::DescriptorBindingFlags bindingFlags;
        uint32_t size;
        ShaderResource(const std::string& inName,uint32_t inSet,uint32_t inBinding,uint32_t inCount,vk::DescriptorType inType,const vk::ShaderStageFlags& inStages,const vk::DescriptorBindingFlags& inBindingFlags,uint32_t inSize = 0);
    };
}
