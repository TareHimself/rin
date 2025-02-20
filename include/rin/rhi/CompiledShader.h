#pragma once
#include <vector>
#include <vulkan/vulkan.hpp>
#include <unordered_map>
namespace rin::rhi
{
    struct CompiledShader
    {
        std::vector<std::pair<vk::ShaderEXT,vk::ShaderStageFlags>> shaders{};
        std::unordered_map<uint32_t,vk::DescriptorSetLayout> layouts{};
        vk::PipelineLayout pipelineLayout{};
    };
}
