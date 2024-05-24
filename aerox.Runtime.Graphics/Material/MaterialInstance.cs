using aerox.Runtime.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics.Material;

using static Vulkan;

/// <summary>
///     Abstracts shaders, pipelines and descriptors
/// </summary>
public class MaterialInstance : MultiDisposable
{
    /// <summary>
    ///     The type of descriptor set in a material
    /// </summary>
    public enum SetType
    {
        Runtime,
        Global,
        Static,
        Dynamic,
        Frame
    }

    // /// <summary>
    // ///     The type of this material
    // /// </summary>
    // public enum Type
    // {
    //     Unknown,
    //     Lit,
    //     Unlit,
    //     Translucent,
    //     Widget,
    //     Compute
    // }

    private readonly VkPipeline _pipeline;
    private readonly VkPipelineLayout _pipelineLayout;
    private readonly ShaderResources _resources;
    private readonly Dictionary<SetType, VkDescriptorSetLayout> _setLayouts;
    private readonly Dictionary<SetType, DescriptorSet> _sets;
    public MaterialInstance(ShaderResources inResources, VkPipelineLayout inPipelineLayout,
        VkPipeline inPipeline, Dictionary<SetType, DescriptorSet> inSets,
        Dictionary<SetType, VkDescriptorSetLayout> inSetLayouts)
    {
        _resources = inResources;
        _pipelineLayout = inPipelineLayout;
        _pipeline = inPipeline;
        _sets = inSets;
        _setLayouts = inSetLayouts;
    }

    protected override void OnDispose(bool isManual)
    {
        var subsystem = Runtime.Instance.GetModule<GraphicsModule>();
        subsystem.WaitDeviceIdle();
        var device = subsystem.GetDevice();
        unsafe
        {
            vkDestroyPipeline(device, _pipeline, null);
            vkDestroyPipelineLayout(device, _pipelineLayout, null);
            foreach (var kv in _setLayouts) vkDestroyDescriptorSetLayout(device, kv.Value, null);
            _setLayouts.Clear();
            foreach (var set in _sets) set.Value.Dispose();
            _sets.Clear();
        }
    }

    /// <summary>
    ///     Binds a <see cref="Texture" /> to this <see cref="MaterialInstance" />
    /// </summary>
    public MaterialInstance BindTexture(string id, Texture texture)
    {
        if (!_resources.images.TryGetValue(id, out var resource)) throw new UnknownParameterException(id);

        var set = _sets[(SetType)resource.set];
        set.WriteTexture(resource.binding, texture, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);

        return this;
    }

    /// <summary>
    ///     Binds a <see cref="Texture" /> array to this <see cref="MaterialInstance" />
    /// </summary>
    public MaterialInstance BindTextureArray(string id, Texture[] textures)
    {
        if (!_resources.images.TryGetValue(id, out var resource)) throw new UnknownParameterException(id);

        var set = _sets[(SetType)resource.set];
        set.WriteTextures(resource.binding, textures, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);

        return this;
    }

    /// <summary>
    ///     Binds an <see cref="Image" /> to this <see cref="MaterialInstance" />
    /// </summary>
    public MaterialInstance BindImage(string id, DeviceImage image, DescriptorSet.ImageType type, VkSampler sampler)
    {
        if (!_resources.images.TryGetValue(id, out var resource)) throw new UnknownParameterException(id);

        var set = _sets[(SetType)resource.set];
        set.WriteImage(resource.binding, image, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL, type, sampler);

        return this;
    }


    /// <summary>
    ///     Binds an <see cref="Image" /> array to this <see cref="MaterialInstance" />
    /// </summary>
    public bool BindImageArray(string id, DeviceImage[] images, DescriptorSet.ImageType type, VkSampler sampler)
    {
        if (!_resources.images.TryGetValue(id, out var resource)) return false;

        var set = _sets[(SetType)resource.set];
        set.WriteImages(resource.binding, images, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL, type,
            sampler);

        return true;
    }

    /// <summary>
    ///     Binds an <see cref="Buffer" /> to this <see cref="MaterialInstance" />
    /// </summary>
    public MaterialInstance BindBuffer(string id, DeviceBuffer buffer,
        DescriptorSet.BufferType type = DescriptorSet.BufferType.Uniform, ulong offset = 0)
    {
        if (!_resources.buffers.TryGetValue(id, out var resource)) throw new UnknownParameterException(id);

        var set = _sets[(SetType)resource.set];
        set.WriteBuffer(resource.binding, buffer, type, offset);

        return this;
    }

    /// <summary>
    ///     Pushes a constant this <see cref="MaterialInstance" />
    /// </summary>
    public MaterialInstance Push<T>(VkCommandBuffer commandBuffer, string id, T constant)
    {
        if (!_resources.pushConstants.TryGetValue(id, out var pushConstant)) throw new UnknownParameterException(id);

        unsafe
        {
            vkCmdPushConstants(commandBuffer, _pipelineLayout, pushConstant.flags, pushConstant.offset,
                pushConstant.size, &constant);
            return this;
        }
    }

    /// <summary>
    ///     Binds this <see cref="MaterialInstance" /> to a <see cref="Frame" />
    /// </summary>
    public void BindTo(Frame frame)
    {
        vkCmdBindPipeline(frame.GetCommandBuffer(), VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _pipeline);
        var sets = _sets.Select(kv => (VkDescriptorSet)kv.Value).ToArray();
        unsafe
        {
            fixed (VkDescriptorSet* pSets = sets)
            {
                vkCmdBindDescriptorSets(frame.GetCommandBuffer(), VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
                    _pipelineLayout, 0, (uint)sets.Length, pSets, 0, null);
            }
        }
    }

    [Serializable]
    public class UnknownParameterException : Exception
    {
        public UnknownParameterException()
        {
        }

        public UnknownParameterException(string parameter)
            : base($"Parameter [{parameter}] does not exist in material")
        {
        }
    }
}