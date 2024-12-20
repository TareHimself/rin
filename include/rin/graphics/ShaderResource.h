#pragma once
#include <string>
#include <vulkan/vulkan.hpp>
namespace rin::graphics
{
    struct ShaderResource
    {
        std::string name{};
        uint32_t set{0};
        uint32_t binding{0};
        uint32_t count{0};
        vk::DescriptorType type{};
        vk::ShaderStageFlags stages{};
        vk::DescriptorBindingFlags bindingFlags{};
        size_t size{0};
    };
}
