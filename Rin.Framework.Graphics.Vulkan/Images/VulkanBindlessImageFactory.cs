using System.Diagnostics;
using Rin.Framework.Buffers;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan.Descriptors;
using Rin.Framework.Shared;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Vulkan.Images;

public class VulkanBindlessImageFactory : IBindlessImageFactory
{
    private const uint MaxTextures = 2048;
    private const uint MaxCubemaps = 512;
    private const uint MaxTextureArrays = 512;
    private const uint SamplerCount = 6;
    private const uint SamplersBinding = 0;
    private const uint TexturesBinding = 1;
    private const uint TextureArraysBinding = 2;
    private const uint CubemapsBinding = 3;
    
    
    private const uint DescriptorTotal = MaxTextures + MaxCubemaps + MaxTextureArrays + SamplerCount;

    private readonly DescriptorAllocator _allocator = new(DescriptorTotal, [
        new PoolSizeRatio(DescriptorType.SampledImage, (float)(MaxTextures + MaxCubemaps + MaxTextureArrays) / DescriptorTotal),
        new PoolSizeRatio(DescriptorType.Sampler, (float)SamplerCount / DescriptorTotal)
    ], VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_UPDATE_AFTER_BIND_BIT);

    private readonly DescriptorSet _descriptorSet;
    private readonly VkDescriptorSetLayout _descriptorSetLayout;
    
    private readonly VkDevice _device;
    private readonly VkPipelineLayout _pipelineLayout;
    private readonly IdFactory _textureIdFactory = new();
    private readonly IdFactory _textureArrayIdFactory = new();
    private readonly IdFactory _cubemapIdFactory = new();
    private readonly Dictionary<ImageHandle, TaskCompletionSource> _pendingTasks = [];
    private readonly Lock _sync = new();
    private readonly List<BindlessTexture> _textures = [];
    private readonly List<BindlessTextureArray> _textureArrays = [];
    private readonly List<BindlessCubemap> _cubemaps = [];
    private readonly IDisposableVulkanTexture _defaultTexture;
    private readonly IDisposableVulkanTextureArray _defaultTextureArray;
    private readonly IDisposableVulkanCubemap _defaultCubemap;
    private uint _count;
    private VulkanGraphicsModule _module;
    
    public VulkanBindlessImageFactory(VulkanGraphicsModule graphicsModule,in VkDevice device)
    {
        _module = graphicsModule;
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
                    flags
                )
                .AddBinding(
                    TextureArraysBinding,
                    DescriptorType.SampledImage,
                    ShaderStage.All,
                    MaxTextureArrays,
                    flags
                )
                .AddBinding(
                    CubemapsBinding,
                    DescriptorType.SampledImage,
                    ShaderStage.All,
                    MaxCubemaps,
                    flags
                )
                .Build();

            _descriptorSet = _allocator.Allocate(_descriptorSetLayout);

            for (var filter = 0; filter < 2; filter++)
            for (var tiling = 0; tiling < 3; tiling++)
                _descriptorSet.WriteSampler(SamplersBinding, new SamplerSpec
                {
                    Filter = (ImageFilter)filter,
                    Tiling = (ImageTiling)tiling
                }, (uint)(filter * 3 + tiling));

            _descriptorSet.Update();
            _pipelineLayout = _device.CreatePipelineLayout([_descriptorSetLayout]);
        }

        _textureIdFactory.NewId(); // 0 is invalid id
        _textureArrayIdFactory.NewId(); // 0 is invalid id
        _cubemapIdFactory.NewId(); // 0 is invalid id
        _textures.Add(new BindlessTexture());
        _textureArrays.Add(new BindlessTextureArray());
        _cubemaps.Add(new BindlessCubemap());
        var extent = new Extent2D(1, 1);
        var format = ImageFormat.RGBA8;
        _defaultTexture = _module.CreateVulkanTexture(extent, format, usage: ImageUsage.Sampled);
        _defaultTextureArray = _module.CreateVulkanTextureArray(extent, format,1, usage: ImageUsage.Sampled);
        _defaultCubemap = _module.CreateVulkanCubemap(extent, format, usage: ImageUsage.Sampled);
    }
    
    private void UpdateHandles(params ImageHandle[] handles)
    {
        List<BindlessResource> pendingToClear = [];
        foreach (var handle in handles)
        {
            switch (handle.Type)
            {
                case ImageType.Texture:
                {
                    var resource = _textures[handle.Id];
                    
                    Debug.Assert(resource.Source is not null);
                    
                    _descriptorSet.WriteSampledImage(TexturesBinding,resource.Source,ImageLayout.ShaderReadOnly, (uint)handle.Id);
                    
                    pendingToClear.Add(resource);
                }
                    break;
                case ImageType.Cubemap:
                {
                    var resource = _cubemaps[handle.Id];
                    
                    Debug.Assert(resource.Source is not null);
                    
                    _descriptorSet.WriteSampledCubemap(CubemapsBinding,resource.Source,ImageLayout.ShaderReadOnly, (uint)handle.Id);
                    
                    pendingToClear.Add(resource);
                }
                    break;
                case ImageType.TextureArray:
                {
                    var resource = _textureArrays[handle.Id];
                    
                    Debug.Assert(resource.Source is not null);
                    
                    _descriptorSet.WriteSampledImageArray(TexturesBinding,resource.Source,ImageLayout.ShaderReadOnly, (uint)handle.Id);
                    
                    pendingToClear.Add(resource);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        _descriptorSet.Update();
        
        
        foreach (var resource in pendingToClear)
        {
            if (resource.State == BindlessResourceState.Uploading)
            {
                _pendingTasks[resource.Handle].SetResult();
                _pendingTasks.Remove(resource.Handle);
            }
            resource.State = BindlessResourceState.Ready;
        }
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
    
    public void Dispose()
    {
        _allocator.Dispose();
        foreach (var resource in _textures)
        {
            resource.Source?.Dispose();
        }
        foreach (var resource in _textureArrays)
        {
            resource.Source?.Dispose();
        }
        foreach (var resource in _cubemaps)
        {
            resource.Source?.Dispose();
        }
        _defaultTexture.Dispose();
        _defaultCubemap.Dispose();
        _defaultTextureArray.Dispose();
        _device.DestroyPipelineLayout(_pipelineLayout);
    }

    public ImageHandle CreateTexture(in Extent2D size, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        var image = _module.CreateVulkanTexture(size, format, mips,usage | ImageUsage.Sampled);

        lock (_sync)
        {
            var handle = new ImageHandle(ImageType.Texture, _textureIdFactory.NewId(out var addToArray));
            var resource = new BindlessTexture
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.PendingBind
            };
            
            if (addToArray)
                _textures.Add(resource);
            else
                _textures[handle.Id] = resource;

            UpdateHandles(resource.Handle);

            return resource.Handle;
        }
    }

    public ImageHandle CreateTextureArray(in Extent2D size, ImageFormat format, uint count, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        var image = _module.CreateVulkanTextureArray(size, format, count,mips,usage | ImageUsage.Sampled);

        lock (_sync)
        {
            var handle = new ImageHandle(ImageType.TextureArray, _textureArrayIdFactory.NewId(out var addToArray));
            var resource = new BindlessTextureArray
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.PendingBind
            };
            
            if (addToArray)
                _textureArrays.Add(resource);
            else
                _textureArrays[handle.Id] = resource;

            UpdateHandles(resource.Handle);

            return resource.Handle;
        }
    }
    
    private void DoAsync(BindlessResource resource,TaskCompletionSource completionSource, Func<Task> action)
    {
        Task.Run(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                lock (_sync)
                {
                    Console.WriteLine(e);
                    completionSource.SetException(e);
                    _pendingTasks.Remove(resource.Handle);
                }
            }
        }).ConfigureAwait(false);
    }

    public ImageHandle CreateCubemap(in Extent2D size, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        var image = _module.CreateVulkanCubemap(size, format, mips,usage | ImageUsage.Sampled);

        lock (_sync)
        {
            var handle = new ImageHandle(ImageType.Cubemap, _textureIdFactory.NewId(out var addToArray));
            var resource = new BindlessCubemap
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.PendingBind
            };
            
            if (addToArray)
                _cubemaps.Add(resource);
            else
                _cubemaps[handle.Id] = resource;

            UpdateHandles(resource.Handle);

            return resource.Handle;
        }
    }

    private Task HandleAsyncBindless<TBindlessResource>(out ImageHandle handle,List<TBindlessResource> list, Func<TBindlessResource,TaskCompletionSource,Task> createAsyncTask) where TBindlessResource : BindlessResource, new()
    {
        var resource = new TBindlessResource
        {
            Handle = ImageHandle.InvalidTextureArray,
            State = BindlessResourceState.Uploading
        };
        
        var completionSource = new TaskCompletionSource();
        var task = completionSource.Task;
        var idFactory = resource switch
        {
            BindlessTexture => _textureIdFactory,
            BindlessTextureArray => _textureArrayIdFactory,
            BindlessCubemap => _cubemapIdFactory,
            _ => throw new ArgumentOutOfRangeException()
        };
        lock (_sync)
        {
            handle = new ImageHandle(resource switch
            {
                BindlessTexture => ImageType.Texture,
                BindlessTextureArray => ImageType.TextureArray,
                BindlessCubemap => ImageType.Cubemap,
                _ => throw new ArgumentOutOfRangeException(nameof(list), list, null)
            }, idFactory.NewId(out var addToArray));
            if (addToArray)
                list.Add(resource);
            else
                list[handle.Id] = resource;

            _pendingTasks.Add(handle, completionSource);
            resource.Handle = handle;
        }
        
        DoAsync(resource,completionSource,() => createAsyncTask(resource, completionSource));

        return task;
    }
    public Task CreateTexture(out ImageHandle handle, IReadOnlyBuffer<byte> data, in Extent2D size, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        var localSize = size;
        return HandleAsyncBindless(out handle, _textures,
            (resource, source) =>
                AsyncCreateTexture(resource, source, data, localSize, format, mips, usage));
    }
    
    private async Task AsyncCreateTexture(BindlessTexture resource, TaskCompletionSource completionSource,
        IReadOnlyBuffer<byte> data,
        Extent2D size, ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        var image = await _module.CreateVulkanTexture(data, size, format,
            mips,usage | ImageUsage.Sampled);
        resource.Source = image;
        lock (_sync)
        {
            // In-case the texture was disposed before we finished creating the image
            if (_textures[resource.Handle.Id] != resource)
            {
                if (!completionSource.Task.IsCanceled) completionSource.SetCanceled();
                image.Dispose();
                return;
            }

            resource.Source = image;
            UpdateHandles(resource.Handle);
        }
    }

    public Task CreateTextureArray(out ImageHandle handle, IReadOnlyBuffer<byte> data, in Extent2D size,
        ImageFormat format,
        uint count,
        bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        var localSize = size;
        return HandleAsyncBindless(out handle, _textureArrays,
            (resource, source) =>
                AsyncCreateTextureArray(resource, source, data, localSize, format, count, mips, usage));
    }

    
    
    private async Task AsyncCreateTextureArray(BindlessTextureArray resource, TaskCompletionSource completionSource,
        IReadOnlyBuffer<byte> data,
        Extent2D size, ImageFormat format,uint count, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        var image = await _module.CreateVulkanTextureArray(data, size, format,
            count,mips,usage | ImageUsage.Sampled);
        resource.Source = image;
        lock (_sync)
        {
            // In-case the texture was disposed before we finished creating the image
            if (_textureArrays[resource.Handle.Id] != resource)
            {
                if (!completionSource.Task.IsCanceled) completionSource.SetCanceled();
                image.Dispose();
                return;
            }

            resource.Source = image;
            UpdateHandles(resource.Handle);
        }
    }

    public Task CreateCubemap(out ImageHandle handle, IReadOnlyBuffer<byte> data,in Extent2D size, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        var localSize = size;
        return HandleAsyncBindless(out handle, _cubemaps,
            (resource, source) =>
                AsyncCreateCubemap(resource, source, data, localSize, format,  mips, usage));
    }
    
    private async Task AsyncCreateCubemap(BindlessCubemap resource, TaskCompletionSource completionSource,
        IReadOnlyBuffer<byte> data,
        Extent2D size, ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        var image = await _module.CreateVulkanCubemap(data, size, format,
            mips,usage | ImageUsage.Sampled);
        resource.Source = image;
        lock (_sync)
        {
            // In-case the texture was disposed before we finished creating the image
            if (_cubemaps[resource.Handle.Id] != resource)
            {
                if (!completionSource.Task.IsCanceled) completionSource.SetCanceled();
                image.Dispose();
                return;
            }

            resource.Source = image;
            UpdateHandles(resource.Handle);
        }
    }

    public ITexture? GetTexture(in ImageHandle handle)
    {
        if(handle.Type != ImageType.Texture || handle.Id == 0 || handle.Id >= _textures.Count) return null;
        lock (_sync)
        {
            var resource = _textures[handle.Id];
            return resource.State != BindlessResourceState.Ready ? null : resource;
        }
    }

    public ITextureArray? GetTextureArray(in ImageHandle handle)
    {
        if(handle.Type != ImageType.TextureArray || handle.Id == 0 || handle.Id >= _textureArrays.Count) return null;
        lock (_sync)
        {
            var resource = _textureArrays[handle.Id];
            return resource.State != BindlessResourceState.Ready ? null : resource;
        }
    }

    public ICubemap? GetCubemap(in ImageHandle handle)
    {
        if (handle.Type != ImageType.Cubemap || handle.Id == 0 || handle.Id >= _cubemaps.Count) return null;
        lock (_sync)
        {
            var resource = _cubemaps[handle.Id];
            return resource.State != BindlessResourceState.Ready ? null : resource;
        }
    }

    public void FreeHandles(params ImageHandle[] handles)
    {
        lock (_sync)
        {
            List<Action> disposes = [];
            foreach (var handle in handles)
            {
                if (handle.Id == 0) continue;

                switch (handle.Type)
                {
                    case ImageType.Texture:
                    {
                        _descriptorSet.WriteSampledImage(TexturesBinding,_defaultTexture,ImageLayout.ShaderReadOnly, (uint)handle.Id);
                        var resource = _textures[handle.Id];
                        if (resource.State == BindlessResourceState.Uploading)
                        {
                            _pendingTasks[handle].SetCanceled();
                            _pendingTasks.Remove(handle);
                        }
                        else
                        {
                            Debug.Assert(resource.Source is not null);
                            disposes.Add(resource.Source.Dispose);
                        }
                        _textures[handle.Id] = new BindlessTexture();
                        _textureIdFactory.FreeId(handle.Id);
                    }
                        break;
                    case ImageType.Cubemap:
                    {
                        _descriptorSet.WriteSampledCubemap(TexturesBinding,_defaultCubemap,ImageLayout.ShaderReadOnly, (uint)handle.Id);
                        var resource = _cubemaps[handle.Id];
                        if (resource.State == BindlessResourceState.Uploading)
                        {
                            
                            _pendingTasks[handle].SetCanceled();
                            _pendingTasks.Remove(handle);
                        }
                        else
                        {
                            Debug.Assert(resource.Source is not null);
                            disposes.Add(resource.Source.Dispose);
                        }
                        _cubemaps[handle.Id] = new BindlessCubemap();
                        _cubemapIdFactory.FreeId(handle.Id);
                    }
                        break;
                    case ImageType.TextureArray:
                    {
                        _descriptorSet.WriteSampledImageArray(TexturesBinding,_defaultTextureArray,ImageLayout.ShaderReadOnly, (uint)handle.Id);
                        var resource = _textureArrays[handle.Id];
                        if (resource.State == BindlessResourceState.Uploading)
                        {
                            _pendingTasks[handle].SetCanceled();
                            _pendingTasks.Remove(handle);
                        }
                        else
                        {
                            Debug.Assert(resource.Source is not null);
                            disposes.Add(resource.Source.Dispose);
                        }
                        _textureArrays[handle.Id] = new BindlessTextureArray();
                        _textureArrayIdFactory.FreeId(handle.Id);
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _descriptorSet.Update();
            foreach (var dispose in disposes)
            {
                dispose();
            }
        }
    }

    public bool IsValid(in ImageHandle handle)
    {
        if(handle.Type != ImageType.Texture || handle.Id == 0 || handle.Id >= _textures.Count) return false;
        return true;
    }

    public uint GetMaxTextures()
    {
        return MaxTextures;
    }

    public uint GetMaxTextureArrays()
    {
        return MaxTextureArrays;
    }

    public uint GetMaxCubemaps()
    {
        return MaxCubemaps;
    }

    public uint GetTextureCount()
    {
        lock (_sync)
        {
            return (uint)_textures.Count;
        }
    }

    public uint GetTextureArrayCount()
    {
        lock (_sync)
        {
            return (uint)_textureArrays.Count;
        }
    }

    public uint GetCubemapCount()
    {
        lock (_sync)
        {
            return (uint)_cubemaps.Count;
        }
    }
}