#pragma once
#include <vector>
#include <vulkan/vulkan.hpp>
#include <map>

struct CompiledShader
{
    std::vector<std::pair<vk::ShaderStageFlagBits,vk::ShaderEXT>> shaders{};
    std::map<uint32_t,vk::DescriptorSetLayout> descriptorLayouts{};
    vk::PipelineLayout pipelineLayout{};
};
