#include "aerox/graphics/descriptors/DescriptorLayoutStore.hpp"

#include <ashl/utils.hpp>

#include "aerox/graphics/GraphicsModule.hpp"

vk::DescriptorSetLayout DescriptorLayoutStore::CreateNew(const vk::DescriptorSetLayoutCreateInfo& key)
{
    return GraphicsModule::Get()->GetDevice().createDescriptorSetLayout(key);
}

uint64_t DescriptorLayoutStore::GetInternalKey(const vk::DescriptorSetLayoutCreateInfo& key)
{
    uint64_t id{0};
    auto next = static_cast<const vk::DescriptorSetLayoutBindingFlagsCreateInfo*>(key.pNext);
    for(auto i  = 0; i < key.bindingCount; i++)
    {
        auto binding = *(key.pBindings + i);
            
        id = ashl::hashCombine(
            id,
            binding.binding,
            binding.descriptorCount,
            static_cast<int>(binding.descriptorType),
            static_cast<uint32_t>(binding.stageFlags),
            static_cast<uint32_t>(*(next->pBindingFlags + i))
            );
    }

    return id;
}
