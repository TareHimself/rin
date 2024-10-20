#pragma once
#include "rin/core/Store.hpp"
#include "vulkan/vulkan.hpp"

class DescriptorLayoutStore : public Store<vk::DescriptorSetLayout, uint64_t, vk::DescriptorSetLayoutCreateInfo>
{
public:
    vk::DescriptorSetLayout CreateNew(const vk::DescriptorSetLayoutCreateInfo& key) override;
    uint64_t GetInternalKey(const vk::DescriptorSetLayoutCreateInfo& key) override;
};
