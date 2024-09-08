#pragma once
#include "DescriptorPool.hpp"
#include "PoolSizeRatio.hpp"
#include "vulkan/vulkan.hpp"


namespace aerox::graphics
{
    
    
    class DescriptorAllocator : public Disposable
    {
        
        // private readonly List<DescriptorPool> _fullPools = new();
        // private readonly PoolSizeRatio[] _ratios;
        // private readonly List<DescriptorPool> _readyPools = new();
        // private uint _setsPerPool;
        vk::Device _device{};
        std::unordered_map<DescriptorPool*,Shared<DescriptorPool>> _pools{};
        std::set<DescriptorPool*> _fullPools{};
        std::set<DescriptorPool*> _readyPools{};
        std::vector<PoolSizeRatio> _ratios{};
        vk::DescriptorPoolCreateFlags _poolCreateFlags{};
        uint32_t _setsPerPool;

        void DestroyPools();
        void ClearPools();
        Shared<DescriptorPool> GetPool();
        Shared<DescriptorPool> CreatePool();
    public:

        DescriptorAllocator(uint32_t maxSets,const std::vector<PoolSizeRatio>& poolRatios,const vk::DescriptorPoolCreateFlags& poolCreateFlags = {});
        
        Shared<DescriptorSet> Allocate(const vk::DescriptorSetLayout& layout, const std::vector<uint32_t>& variableCount = {});

        static Shared<DescriptorAllocator> New(uint32_t maxSets,const std::vector<PoolSizeRatio>& poolRatios,const vk::DescriptorPoolCreateFlags& poolCreateFlags = vk::DescriptorPoolCreateFlagBits::eUpdateAfterBind);

        void OnDispose(bool manual) override;
    };
}
