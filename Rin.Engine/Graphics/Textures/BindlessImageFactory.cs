using Rin.Engine.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.Textures;

public class BindlessImageFactory : IBindlessImageFactory
{
    private const uint MaxTextures = 2048;

    private readonly DescriptorAllocator _allocator = new(MaxTextures, [
        new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, 1.0f)
    ], VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_UPDATE_AFTER_BIND_BIT);

    private readonly DescriptorSet _descriptorSet;

    private readonly IdFactory _imageIdFactory = new();
    private readonly Dictionary<ImageHandle, TaskCompletionSource> _pendingTextures = [];
    private readonly Lock _sync = new();
    private readonly List<Texture> _textures = [];
    private uint _count;

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

    public BindlessImageFactory()
    {
        {
            const VkDescriptorBindingFlags flags = VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT |
                                                   VkDescriptorBindingFlags
                                                       .VK_DESCRIPTOR_BINDING_VARIABLE_DESCRIPTOR_COUNT_BIT |
                                                   VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT;
            var layout = new DescriptorLayoutBuilder().AddBinding(
                0,
                VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                VkShaderStageFlags.VK_SHADER_STAGE_ALL,
                MaxTextures,
                flags
            ).Build();

            _descriptorSet = _allocator.Allocate(layout, MaxTextures);
        }

        var defaultTex = new Texture();
        _textures.Add(defaultTex);
        _textures.Add(defaultTex);
        _imageIdFactory.NewId();
        _imageIdFactory.NewId();

        LoadDefaultTexture().ConfigureAwait(false);
    }

    public DescriptorSet GetDescriptorSet()
    {
        return _descriptorSet;
    }

    public ImageHandle CreateTexture(in Extent3D size, ImageFormat format, ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mips = false, ImageUsage usage = ImageUsage.None,
        string? debugName = null)
    {
        var image = SGraphicsModule.Get().CreateDeviceImage(size, format,
            usage | ImageUsage.Sampled, mips, debugName ?? "Texture");

        lock (_sync)
        {
            var textureHandle = new ImageHandle(ImageType.Image, _imageIdFactory.NewId(out var addToArray));
            var boundText = new Texture(textureHandle, image, filter, tiling, mips, debugName ?? "Texture")
            {
                Uploading = true
            };

            if (addToArray)
                _textures.Add(boundText);
            else
                _textures[textureHandle.Id] = boundText;

            UpdateTextures(boundText.Handle);

            return boundText.Handle;
        }
    }

    public (ImageHandle handle, Task task) CreateTexture(Buffer<byte> data, Extent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mips = false, ImageUsage usage = ImageUsage.None,
        string? debugName = null)
    {
        // var image = await SGraphicsModule.Get().CreateImage(data, size, format,
        //     VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT, mips, filter, debugName);
        var boundText = new Texture(ImageHandle.InvalidImage, filter, tiling, mips, debugName ?? "Texture")
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
                AsyncCreateTexture(boundText, completionSource, data, size, format, filter, mips, usage, debugName))
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

    public void FreeTextures(params ImageHandle[] textureIds)
    {
        lock (_sync)
        {
            List<ImageWrite> writes = [];

            var sampler = new SamplerSpec
            {
                Filter = ImageFilter.Linear,
                Tiling = ImageTiling.Repeat
            };
            var defaultTexture = _textures[0].Image;

            foreach (var textureId in textureIds)
            {
                if (textureId.Id == 0) continue;

                var info = _textures[textureId.Id];

                if (info.Uploading)
                {
                    _textures[textureId.Id] = new Texture();
                    _pendingTextures[textureId].SetCanceled();
                    _pendingTextures.Remove(textureId);
                    continue;
                }

                if (!info.Uploaded) continue;
                _count--;
                info.Image?.Dispose();
                _textures[textureId.Id] = new Texture();
                _imageIdFactory.FreeId(textureId.Id);

                if (defaultTexture != null)
                    writes.Add(new ImageWrite(defaultTexture, ImageLayout.ShaderReadOnly, DescriptorImageType.Sampled,
                        sampler)
                    {
                        Index = (uint)textureId.Id
                    });
            }

            if (writes.Count != 0) _descriptorSet.WriteImages(0, writes.ToArray());
        }
    }

    public ITexture? GetTexture(ImageHandle imageHandle)
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
                _textures[i] = new Texture();
            }
        }

        _textures.Clear();
    }


    private async Task AsyncCreateTexture(Texture boundText, TaskCompletionSource completionSource, Buffer<byte> data,
        Extent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear, bool mips = false, ImageUsage usage = ImageUsage.None,
        string? debugName = null)
    {
        try
        {
            var image = await SGraphicsModule.Get().CreateDeviceImage(data, size, format,
                usage | ImageUsage.Sampled, mips, filter, debugName ?? "Texture");
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
        using var imgData = HostImage.Create(SEngine.Get().Sources.Read("Engine/Textures/default.png"));

        var tex = _textures[0];
        tex.DebugName = "Default Texture";
        tex.Image = await SGraphicsModule.Get().CreateDeviceImage(imgData, ImageUsage.Sampled, true,
            debugName: tex.DebugName);

        List<ImageWrite> writes = [];

        var sampler = new SamplerSpec
        {
            Filter = ImageFilter.Linear,
            Tiling = ImageTiling.Repeat
        };

        for (var i = 0; i < MaxTextures; i++)
        {
            var info = i < _textures.Count ? _textures[i] : null;

            if (info?.Uploaded == true && i != 0) continue;

            writes.Add(new ImageWrite(tex.Image, ImageLayout.ShaderReadOnly, DescriptorImageType.Sampled, sampler)
            {
                Index = (uint)i
            });
        }

        if (writes.Count != 0) _descriptorSet.WriteImages(0, writes.ToArray());

        tex.Uploaded = true;
    }

    private void UpdateTextures(params ImageHandle[] textureIds)
    {
        List<ImageWrite> writes = [];
        List<Texture> infos = [];
        foreach (var textureId in textureIds)
        {
            var info = _textures[textureId.Id];
            if (info.Uploaded || info.Image is null) continue;

            writes.Add(new ImageWrite(info.Image, ImageLayout.ShaderReadOnly, DescriptorImageType.Sampled,
                new SamplerSpec
                {
                    Filter = info.Filter,
                    Tiling = info.Tiling
                })
            {
                Index = (uint)textureId.Id
            });

            infos.Add(info);
        }

        if (writes.Count != 0) _descriptorSet.WriteImages(0, writes.ToArray());
        foreach (var texture in infos)
        {
            texture.Uploaded = true;
            texture.Uploading = false;
        }
    }
}