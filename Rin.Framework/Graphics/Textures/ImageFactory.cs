using Rin.Framework.Graphics.Descriptors;
using Rin.Framework.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Framework.Graphics.Textures;

public class ImageFactory : IImageFactory
{
    private const uint MaxTextures = 2048;
    private const uint SamplerCount = 6;
    private const uint SamplersBinding = 0;
    private const uint TexturesBinding = 1;
    private const uint DescriptorTotal = MaxTextures + SamplerCount;

    private readonly DescriptorAllocator _allocator = new(DescriptorTotal, [
        new PoolSizeRatio(DescriptorType.SampledImage, (float)MaxTextures / DescriptorTotal),
        new PoolSizeRatio(DescriptorType.Sampler, (float)SamplerCount / DescriptorTotal)
    ], VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_UPDATE_AFTER_BIND_BIT);

    private readonly DescriptorSet _descriptorSet;
    private readonly VkDescriptorSetLayout _descriptorSetLayout;

    // public class BoundTexture : ITexture
    // {
    //     public readonly IDeviceImage? Image;
    //     public readonly ImageFilter Filter;
    //     public readonly ImageTiling Tiling;
    //     public bool MipMapped = false;
    //     public string DebugName;
    //     public bool Valid => Image != null;
    //
    //     public BoundTexture()
    //     {
    //         DebugName = "";
    //     }
    //
    //     public BoundTexture(IDeviceImage image, ImageFilter filter, ImageTiling tiling, bool mipMapped, string debugName)
    //     {
    //         Image = image;
    //         Filter = filter;
    //         Tiling = tiling;
    //         MipMapped = mipMapped;
    //         DebugName = debugName;
    //     }
    // }

    private readonly VkDevice _device;

    private readonly IdFactory _imageIdFactory = new();
    private readonly Dictionary<ImageHandle, TaskCompletionSource> _pendingTextures = [];
    private readonly VkPipelineLayout _pipelineLayout;
    private readonly Lock _sync = new();
    private readonly List<BindlessImage> _textures = [];
    private uint _count;

    public ImageFactory(in VkDevice device)
    {
        _device = device;
        {
            const DescriptorBindingFlags flags = DescriptorBindingFlags.PartiallyBound |
                                                 DescriptorBindingFlags.UpdateAfterBind;
            _descriptorSetLayout = new DescriptorLayoutBuilder()
                .AddBinding(
                    SamplersBinding,
                    DescriptorType.Sampler,
                    ShaderStage.All,
                    SamplerCount,
                    flags
                )
                .AddBinding(
                    TexturesBinding,
                    DescriptorType.SampledImage,
                    ShaderStage.All,
                    MaxTextures,
                    flags |
                    DescriptorBindingFlags.Variable
                )
                .Build();

            _descriptorSet = _allocator.Allocate(_descriptorSetLayout, MaxTextures);

            for (var filter = 0; filter < 2; filter++)
            for (var tiling = 0; tiling < 3; tiling++)
                _descriptorSet.WriteSampler(SamplersBinding, new SamplerSpec
                {
                    Filter = (ImageFilter)filter,
                    Tiling = (ImageTiling)tiling
                }, (uint)(filter * 3 + tiling));

            _descriptorSet.Update();

            // var pipelineLayoutCreateInfo = new VkPipelineLayoutCreateInfo
            // {
            //     sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
            //     setLayoutCount = 1,
            //     pSetLayouts = &setLayout
            // };
            //
            // var pipelineLayout = new VkPipelineLayout();
            // vkCreatePipelineLayout(_device, &pipelineLayoutCreateInfo, null, &pipelineLayout);
            // _pipelineLayout = pipelineLayout;

            _pipelineLayout = _device.CreatePipelineLayout([_descriptorSetLayout]);
        }

        var defaultTex = new BindlessImage();
        _textures.Add(defaultTex);
        _textures.Add(defaultTex);
        _imageIdFactory.NewId();
        _imageIdFactory.NewId();

        LoadDefaultTexture().ConfigureAwait(false);
    }

    public void Bind(in VkCommandBuffer cmd)
    {
        cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _pipelineLayout, [_descriptorSet]);
        cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_COMPUTE, _pipelineLayout, [_descriptorSet]);
    }

    public DescriptorSet GetDescriptorSet()
    {
        return _descriptorSet;
    }

    public VkPipelineLayout GetPipelineLayout()
    {
        return _pipelineLayout;
    }

    public (ImageHandle handle, IDeviceImage image) CreateTexture(in Extent3D size, ImageFormat format,
        bool mips = false,
        ImageUsage usage = ImageUsage.None,
        string? debugName = null)
    {
        var image = SGraphicsModule.Get().CreateDeviceImage(size, format,
            usage | ImageUsage.Sampled, mips, debugName ?? "Texture");

        lock (_sync)
        {
            var textureHandle = new ImageHandle(ImageType.Image, _imageIdFactory.NewId(out var addToArray));
            var boundText = new BindlessImage(textureHandle, image, mips, debugName ?? "Texture")
            {
                Uploading = true
            };

            if (addToArray)
                _textures.Add(boundText);
            else
                _textures[textureHandle.Id] = boundText;

            UpdateTextures(boundText.Handle);

            return (boundText.Handle, image);
        }
    }

    public (ImageHandle handle, Task task) CreateTexture(Buffer<byte> data, Extent3D size, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None,
        string? debugName = null)
    {
        // var image = await SGraphicsModule.Get().CreateImage(data, size, format,
        //     VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT, mips, filter, debugName);
        var boundText = new BindlessImage(ImageHandle.InvalidImage, mips, debugName ?? "Texture")
        {
            Uploading = true
        };

        ImageHandle imageHandle;
        var completionSource = new TaskCompletionSource();
        var task = completionSource.Task;
        lock (_sync)
        {
            imageHandle = new ImageHandle(ImageType.Image, _imageIdFactory.NewId(out var addToArray));
            if (addToArray)
                _textures.Add(boundText);
            else
                _textures[imageHandle.Id] = boundText;

            _pendingTextures.Add(imageHandle, completionSource);
            boundText.Handle = imageHandle;
        }

        Task.Run(() =>
                AsyncCreateTexture(boundText, completionSource, data, size, format, mips, usage, debugName))
            .ConfigureAwait(false);

        return (imageHandle, task);
    }

    public Task? GetPendingTexture(in ImageHandle imageHandle)
    {
        lock (_sync)
        {
            if (_pendingTextures.TryGetValue(imageHandle, out var src)) return src.Task;
        }

        return null;
    }

    public bool IsTextureReady(in ImageHandle imageHandle)
    {
        lock (_sync)
        {
            return _textures[imageHandle.Id].Uploaded;
        }
    }

    public void FreeHandles(params ImageHandle[] textureIds)
    {
        lock (_sync)
        {
            var defaultTexture = _textures[0].Image;

            foreach (var textureId in textureIds)
            {
                if (textureId.Id == 0) continue;

                var info = _textures[textureId.Id];

                if (info.Uploading)
                {
                    _textures[textureId.Id] = new BindlessImage();
                    _pendingTextures[textureId].SetCanceled();
                    _pendingTextures.Remove(textureId);
                    continue;
                }

                if (!info.Uploaded) continue;
                _count--;
                info.Image?.Dispose();
                _textures[textureId.Id] = new BindlessImage();
                _imageIdFactory.FreeId(textureId.Id);

                if (defaultTexture != null)
                    _descriptorSet.WriteSampledImage(TexturesBinding, defaultTexture, ImageLayout.ShaderReadOnly,
                        (uint)textureId.Id);
            }

            _descriptorSet.Update();
        }
    }

    public IBindlessImage? GetTexture(ImageHandle imageHandle)
    {
        return IsHandleValid(imageHandle) ? _textures[imageHandle.Id] : null;
    }

    public IDeviceImage? GetTextureImage(ImageHandle imageHandle)
    {
        return GetTexture(imageHandle)?.Image;
    }

    public bool IsHandleValid(in ImageHandle imageHandle)
    {
        // 0 is reserved
        if (imageHandle.Id == 0) return false;

        return !_imageIdFactory.IsFree(imageHandle.Id);
    }

    public uint GetMaxTextures()
    {
        return MaxTextures;
    }

    public uint GetTexturesCount()
    {
        return _count;
    }

    public void Dispose()
    {
        _allocator.Dispose();
        for (var i = 0; i < _textures.Count; i++)
        {
            var bound = _textures[i];
            if (bound.Uploaded)
            {
                bound.Image?.Dispose();
                bound.Uploaded = false;
                bound.Uploading = false;
            }
            else if (bound.Uploading)
            {
                _textures[i] = new BindlessImage();
            }
        }

        _textures.Clear();
        unsafe
        {
            vkDestroyPipelineLayout(_device, _pipelineLayout, null);
        }
    }


    private async Task AsyncCreateTexture(BindlessImage boundText, TaskCompletionSource completionSource,
        Buffer<byte> data,
        Extent3D size, ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None,
        string? debugName = null)
    {
        try
        {
            var image = await SGraphicsModule.Get().CreateDeviceImage(data, size, format,
                usage | ImageUsage.Sampled, mips, debugName: debugName ?? "Texture");
            boundText.Image = image;
            lock (_sync)
            {
                // In-case the texture was disposed before we finished creating the image
                if (_textures[boundText.Handle.Id] != boundText)
                {
                    if (!completionSource.Task.IsCanceled) completionSource.SetCanceled();
                    image.Dispose();
                    return;
                }

                boundText.Image = image;
                UpdateTextures(boundText.Handle);
                completionSource.SetResult();
            }
        }
        catch (Exception e)
        {
            lock (_sync)
            {
                Console.WriteLine(e);
                completionSource.SetException(e);
                _pendingTextures.Remove(boundText.Handle);
            }
        }
    }

    private async Task LoadDefaultTexture()
    {
        using var imgData = HostImage.Create(SApplication.Get().Sources.Read("Framework/Textures/default.png"));

        var tex = _textures[0];
        tex.DebugName = "Default Texture";
        tex.Image = await SGraphicsModule.Get().CreateDeviceImage(imgData, ImageUsage.Sampled, true,
            debugName: tex.DebugName);
        
        for (var i = 0; i < MaxTextures; i++)
        {
            var info = i < _textures.Count ? _textures[i] : null;

            if (info?.Uploaded == true && i != 0) continue;
            _descriptorSet.WriteSampledImage(TexturesBinding, tex.Image, ImageLayout.ShaderReadOnly, (uint)i);
        }

        _descriptorSet.Update();
        tex.Uploaded = true;
    }

    private void UpdateTextures(params ImageHandle[] textureIds)
    {
        List<BindlessImage> infos = [];
        foreach (var textureId in textureIds)
        {
            var info = _textures[textureId.Id];
            if (info.Uploaded || info.Image is null) continue;

            _descriptorSet.WriteSampledImage(TexturesBinding, info.Image, ImageLayout.ShaderReadOnly,
                (uint)textureId.Id);
            infos.Add(info);
        }

        _descriptorSet.Update();
        foreach (var texture in infos)
        {
            texture.Uploaded = true;
            texture.Uploading = false;
        }
    }
}