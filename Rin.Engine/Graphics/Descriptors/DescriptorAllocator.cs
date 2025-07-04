using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics.Descriptors;

public class DescriptorAllocator : Disposable
{
    private readonly VkDevice _device;
    private readonly HashSet<DescriptorPool> _fullPools = [];
    private readonly VkDescriptorPoolCreateFlags _poolCreateFlags;
    private readonly PoolSizeRatio[] _ratios;
    private readonly HashSet<DescriptorPool> _readyPools = [];
    private uint _setsPerPool;

    public DescriptorAllocator(uint maxSets, PoolSizeRatio[] poolRatios,
        VkDescriptorPoolCreateFlags poolCreateFlags = 0)
    {
        _ratios = poolRatios;
        _setsPerPool = maxSets;
        _device = SGraphicsModule.Get().GetDevice();
        _poolCreateFlags = poolCreateFlags;
    }

    protected override void OnDispose(bool isManual)
    {
        DestroyPools();
    }

    public DescriptorSet Allocate(VkDescriptorSetLayout layout, params uint[] variableCount)
    {
        var targetPool = GetPool();
        DescriptorSet set;
        try
        {
            set = targetPool.Allocate(layout, variableCount);
        }
        catch (Exception)
        {
            _fullPools.Add(targetPool);
            _readyPools.Remove(targetPool);
            targetPool = GetPool();
            set = targetPool.Allocate(layout);
        }

        _readyPools.Add(targetPool);

        return set;
    }

    public void DestroyPools()
    {
        foreach (var pool in _fullPools) pool.Dispose();

        _fullPools.Clear();


        foreach (var pool in _readyPools) pool.Dispose();

        _readyPools.Clear();
    }

    public void ClearPools()
    {
        foreach (var pool in _readyPools) pool.Reset();

        foreach (var pool in _fullPools)
        {
            pool.Reset();
            _readyPools.Add(pool);
        }

        _fullPools.Clear();
    }

    private DescriptorPool GetPool()
    {
        DescriptorPool pool;
        if (_readyPools.Count > 0)
        {
            pool = _readyPools.First();
        }
        else
        {
            pool = CreatePool(_setsPerPool, _ratios);
            _setsPerPool = (uint)(_setsPerPool * 1.5);
            if (_setsPerPool > 4092) _setsPerPool = 4092;
            _readyPools.Add(pool);
        }

        return pool;
    }

    private unsafe DescriptorPool CreatePool(uint setCount, PoolSizeRatio[] poolRatios)
    {
        var poolSizes = poolRatios.Select(r =>
        {
            VkDescriptorPoolSize size = default;
            size.type = r.type.ToVk();
            size.descriptorCount = (uint)(r.ratio * setCount);
            return size;
        }).ToArray();


        var poolInfo = new VkDescriptorPoolCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
            flags = _poolCreateFlags,
            maxSets = setCount
        };
        fixed (VkDescriptorPoolSize* pPoolSizes = poolSizes)
        {
            poolInfo.pPoolSizes = pPoolSizes;
            poolInfo.poolSizeCount = (uint)poolSizes.Length;
        }

        VkDescriptorPool pool;
        vkCreateDescriptorPool(_device, &poolInfo, null, &pool);
        return new DescriptorPool(_device, pool);
    }
}