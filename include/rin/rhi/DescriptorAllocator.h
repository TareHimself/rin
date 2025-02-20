#pragma once
#include <unordered_set>
#include "PoolSizeRatio.h"
#include "rin/core/delegates/DelegateList.h"

namespace std {
    template <>
    struct hash<vk::DescriptorPool> {
        auto operator()(const vk::DescriptorPool &xyz) const noexcept -> size_t {
            auto data = static_cast<VkDescriptorPool>(xyz);
            constexpr std::hash<VkDescriptorPool> hasher;
            return hasher(data);
        }
    };
}
namespace rin::rhi
{
    
    class DescriptorSet;
    class DescriptorAllocator
    {
        std::unordered_set<vk::DescriptorPool> _fullPools{};
        std::unordered_set<vk::DescriptorPool> _readyPools{};
        std::unordered_map<vk::DescriptorPool,std::vector<DescriptorSet *>> _sets{};
        uint32_t _setsPerPool{0};
        vk::DescriptorPoolCreateFlags _createFlags{};
        std::vector<PoolSizeRatio> _ratios{};
        vk::Device _device{};
        vk::DescriptorPool GetPool();
        vk::DescriptorPool CreatePool(uint32_t sets, const std::vector<PoolSizeRatio>& ratios) const;
        DescriptorSet * Allocate(const vk::DescriptorPool& pool,const vk::DescriptorSetLayout& layout,const std::vector<uint32_t>& variableCounts = {});
    public:

        DescriptorAllocator(uint32_t maxSets,const std::vector<PoolSizeRatio>& ratios,const vk::DescriptorPoolCreateFlags& poolCreateFlags = {});
        
        ~DescriptorAllocator();
        
        DescriptorSet * Allocate(const vk::DescriptorSetLayout& layout,const std::vector<uint32_t>& variableCounts = {});
        
        /**
         * Reset and destroy all pools
         */
        
        void DestroyPools();
        /**
         * Reset all pools
         */
        void ResetPools();

        DEFINE_DELEGATE_LIST(onReset)
        
    };
}
