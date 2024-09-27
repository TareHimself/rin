#pragma once
#include "DescriptorSet.hpp"
#include "rin/graphics/shaders/Shader.hpp"
#include "vulkan/vulkan.hpp"
class DescriptorPool : public Disposable
{
    vk::DescriptorPool _pool{};
    std::vector<Shared<DescriptorSet>> _descriptors{};
public:
    DescriptorPool(const vk::DescriptorPool& pool);


    Shared<DescriptorSet> Allocate(const vk::DescriptorSetLayout& layout, const std::vector<uint32_t>& variableCount = {});

    void Reset();

    void OnDispose(bool manual) override;
};
