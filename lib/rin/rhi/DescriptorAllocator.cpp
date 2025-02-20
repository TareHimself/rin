#include "rin/rhi/DescriptorAllocator.h"

#include "rin/rhi/GraphicsModule.h"

namespace rin::rhi
{

    vk::DescriptorPool DescriptorAllocator::GetPool()
    {
        vk::DescriptorPool pool{};

        if(!_readyPools.empty())
        {
            pool = *_readyPools.begin();
        }
        else
        {
            pool = CreatePool(_setsPerPool,_ratios);
            _setsPerPool = static_cast<uint32_t>(static_cast<float>(_setsPerPool) * 1.5f);
            if(_setsPerPool > 4092) _setsPerPool = 4092;
            _readyPools.emplace(pool);
        }
        return pool;
    }
    vk::DescriptorPool DescriptorAllocator::CreatePool(uint32_t sets, const std::vector<PoolSizeRatio>& ratios) const
    {
        std::vector<vk::DescriptorPoolSize> poolSizes{};
        poolSizes.reserve(ratios.size());
        for (auto &ratio : ratios)
        {
            poolSizes.emplace_back(ratio.type,static_cast<uint32_t>(ratio.ratio * static_cast<float>(sets)));
        } 
        vk::DescriptorPoolCreateInfo createInfo{_createFlags,sets,poolSizes};
        return _device.createDescriptorPool(createInfo);
    }
    DescriptorSet* DescriptorAllocator::Allocate(const vk::DescriptorPool& pool, const vk::DescriptorSetLayout& layout, const std::vector<uint32_t>& variableCounts)
    {
        if(!_sets.contains(pool))
        {
            _sets.emplace(pool,std::vector<DescriptorSet*>{});
        }
        
        vk::DescriptorSetAllocateInfo allocateInfo{pool,{layout}};

        DescriptorSet * set;
        if(!variableCounts.empty())
        {
            vk::DescriptorSetVariableDescriptorCountAllocateInfo variableInfo{variableCounts};
            allocateInfo.pNext = &variableInfo;
            set = new DescriptorSet(_device.allocateDescriptorSets(allocateInfo).at(0));
        }
        else
        {
            set = new DescriptorSet(_device.allocateDescriptorSets(allocateInfo).at(0));
        }
        _sets.at(pool).emplace_back(set);
        return set;
    }
    DescriptorAllocator::DescriptorAllocator(uint32_t maxSets, const std::vector<PoolSizeRatio>& ratios, const vk::DescriptorPoolCreateFlags& poolCreateFlags)
    {
        _ratios = ratios;
        _setsPerPool = maxSets;
        _device = GraphicsModule::Get()->GetDevice();
        _createFlags = poolCreateFlags;
    }
    DescriptorAllocator::~DescriptorAllocator()
    {
        DestroyPools();
    }
    DescriptorSet* DescriptorAllocator::Allocate(const vk::DescriptorSetLayout& layout, const std::vector<uint32_t>& variableCounts)
    {
        auto pool = GetPool();
        DescriptorSet * set{nullptr};
        try
        {
            set = Allocate(pool,layout,variableCounts);
        }
        catch(...)
        {
            _fullPools.emplace(pool);
            _readyPools.erase(pool);
            pool = GetPool();
            set = Allocate(pool,layout,variableCounts);
        }

        _readyPools.emplace(pool);

        return set;
    }
    void DescriptorAllocator::DestroyPools()
    {
        for (auto &pool : _fullPools)
        {
            for (const auto set : _sets.at(pool))
            {
                delete set;
            }
            _sets.erase(pool);
            _device.destroyDescriptorPool(pool);
        }

        _fullPools.clear();

        for (auto &pool : _readyPools)
        {
            for (const auto set : _sets.at(pool))
            {
                delete set;
            }
            _sets.erase(pool);
            _device.destroyDescriptorPool(pool);
        }

        _readyPools.clear();
    }
    
    void DescriptorAllocator::ResetPools()
    {
        for (auto &pool : _readyPools)
        {
            for (const auto set : _sets.at(pool))
            {
                delete set;
            }
            _sets.erase(pool);
            _device.resetDescriptorPool(pool);
        }

        for (auto &pool : _fullPools)
        {
            for (const auto set : _sets.at(pool))
            {
                delete set;
            }
            _sets.erase(pool);
            _device.resetDescriptorPool(pool);
            _readyPools.emplace(pool);
        }

        _fullPools.clear();
    }
}
