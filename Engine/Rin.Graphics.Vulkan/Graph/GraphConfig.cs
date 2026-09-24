using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Graph;

public class GraphConfig(GraphBuilder builder) : IGraphConfig
{
    public enum ActionType
    {
        Read,
        Write
    }

    public enum DependencyType
    {
        Pass,
        Read,
        Write
    }

    private readonly Dictionary<uint, GraphConfigBuffer> _buffers = [];

    private readonly Dictionary<uint, GraphConfigImage> _images = [];
    private readonly Dictionary<uint, int> _prependedWrites = [];

    public readonly Dictionary<uint, List<Dependency>> PassDependencies = [];
    public readonly Dictionary<uint, List<ResourceAction>> ResourceActions = [];
    public readonly Dictionary<uint, IResourceDescriptor> Resources = [];

    public uint CurrentPassId { get; set; }


    public uint SwapchainImageId { get; set; }

    public uint AddExternalImage(ResourceHandle handle, Action? onDispose = null)
    {
        if (builder.TryReuseExternalImage(handle, onDispose, out var existingId)) return existingId;
        if (ExternalResourceDescriptors.Make(handle, onDispose) is not { } descriptor) return 0;
        var resourceId = builder.MakeId();
        Resources.Add(resourceId, descriptor);
        builder.RememberExternalImage(handle, resourceId);
        return resourceId;
    }

    public uint AddExternalBuffer(in DeviceBufferView view, Action? onDispose = null)
    {
        if (ExternalResourceDescriptors.MakeBuffer(view, onDispose) is not { } descriptor) return 0;
        var resourceId = builder.MakeId();
        Resources.Add(resourceId, descriptor);
        builder.RememberExternalBuffer(view.Buffer, resourceId);
        return resourceId;
    }

    public uint CreateTexture(in Extent2D extent, ImageFormat format, ImageLayout layout)
    {
        return CreateImage(extent, format, layout, 0, ResourceType.Texture);
    }

    public uint CreateTextureArray(in Extent2D extent, ImageFormat format, uint count, ImageLayout layout)
    {
        return CreateImage(extent, format, layout, count, ResourceType.TextureArray);
    }

    public uint CreateCubemap(in Extent2D extent, ImageFormat format, ImageLayout layout)
    {
        return CreateImage(extent, format, layout, 0, ResourceType.Cubemap);
    }


    public uint CreateBuffer(ulong size, GraphBufferUsage usage)
    {
        var resourceId = builder.MakeId();
        // A pass may legitimately have nothing to put in a buffer this frame (e.g. zero meshes on
        // a loading screen) - clamp instead of rejecting, rather than forcing every call site to
        // special-case "empty this frame".
        _buffers.Add(resourceId, new GraphConfigBuffer
        {
            Size = ulong.Max(size, 1),
            Usage = GraphBufferUsageToDeviceUsageFlags(usage)
        });
        UseBuffer(resourceId, usage, ResourceOperation.Write);
        //WriteSingle(resourceId);
        return resourceId;
    }

    public uint UseTexture(uint id, ImageLayout layout, ResourceOperation operation)
    {
        Debug.Assert(id != 0, "Invalid Image Id");
        {
            var dep = new Dependency
            {
                Type = operation switch
                {
                    ResourceOperation.Write => DependencyType.Write,
                    ResourceOperation.Read => DependencyType.Read,
                    _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
                },
                Id = id
            };

            if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
                dependencies.Add(dep);
            else
                PassDependencies.Add(CurrentPassId, [dep]);
        }
        {
            var action = new ResourceAction
            {
                Operation = operation,
                PassId = CurrentPassId,
                Type = GraphResourceKind.Image,
                ImageLayout = layout
            };

            if (ResourceActions.TryGetValue(id, out var passes))
                passes.Add(action);
            else
                ResourceActions.Add(id, [action]);
        }

        {
            if (_images.TryGetValue(id, out var image)) image.Usage |= DeriveImageUsage(layout);
        }

        return id;
    }

    public uint UseTextureArray(uint id, ImageLayout layout, ResourceOperation operation)
    {
        return UseTexture(id, layout, operation);
    }

    public uint UseCubemap(uint id, ImageLayout layout, ResourceOperation operation)
    {
        return UseTexture(id, layout, operation);
    }

    public uint UseBuffer(uint id, GraphBufferUsage usage, ResourceOperation operation)
    {
        Debug.Assert(id != 0, "Invalid Buffer Id");
        {
            var dep = new Dependency
            {
                Type = operation switch
                {
                    ResourceOperation.Write => DependencyType.Write,
                    ResourceOperation.Read => DependencyType.Read,
                    _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
                },
                Id = id
            };

            if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
                dependencies.Add(dep);
            else
                PassDependencies.Add(CurrentPassId, [dep]);
        }
        {
            var action = new ResourceAction
            {
                Operation = operation,
                PassId = CurrentPassId,
                Type = GraphResourceKind.Buffer,
                BufferUsage = usage
            };

            if (ResourceActions.TryGetValue(id, out var passes))
                passes.Add(action);
            else
                ResourceActions.Add(id, [action]);
        }

        if (_buffers.TryGetValue(id, out var configBuffer))
            configBuffer.Usage |= GraphBufferUsageToDeviceUsageFlags(usage);
        return id;
    }

    public uint DependOn(uint passId)
    {
        Debug.Assert(passId != 0, "Invalid Pass Id");

        {
            var dep = new Dependency
            {
                Type = DependencyType.Pass,
                Id = passId
            };

            if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
                dependencies.Add(dep);
            else
                PassDependencies.Add(CurrentPassId, [dep]);
        }
        return passId;
    }


    internal void PrependTextureWrite(uint id, bool discard)
    {
        UseTexture(id, ImageLayout.TransferDst, ResourceOperation.Write);
        MoveLastActionToPrepended(id).Discard = discard;
    }

    internal void PrependBufferWrite(uint id)
    {
        UseBuffer(id, GraphBufferUsage.Transfer, ResourceOperation.Write);
        MoveLastActionToPrepended(id);
    }

    private ResourceAction MoveLastActionToPrepended(uint id)
    {
        var actions = ResourceActions[id];
        var action = actions[^1];
        actions.RemoveAt(actions.Count - 1);
        var index = _prependedWrites.GetValueOrDefault(id);
        actions.Insert(index, action);
        _prependedWrites[id] = index + 1;
        return action;
    }

    private uint CreateImage(in Extent2D extent, ImageFormat format, ImageLayout layout, uint count, ResourceType type)
    {
        Debug.Assert(extent is { Width: > 0, Height: > 0 },
            "all image dimensions must be greater than zero");
        var flags = format switch
        {
            ImageFormat.Depth => ImageCreateFlags.DepthAttachment,
            ImageFormat.Stencil => ImageCreateFlags.StencilAttachment,
            _ => ImageCreateFlags.None
        };
        var resourceId = builder.MakeId();
        _images.Add(resourceId, new GraphConfigImage
        {
            Extent = extent,
            Usage = DeriveImageUsage(layout) | flags,
            Format = format,
            Type = type,
            Count = count
        });
        //Resources.Add(resourceId, descriptor); // We do this at the end for images
        // This is always a write because the image was created here
        // Always use texture for now (no difference)
        UseTexture(resourceId, layout, ResourceOperation.Write);
        return resourceId;
    }

    /// <summary>
    ///     The Vulkan backend's own translation of graph-level buffer intent into creation flags - a Metal
    ///     (or other) backend would derive whatever it needs from the same <see cref="GraphBufferUsage" />
    ///     instead. Buffers pooled by <see cref="ResourcePool" /> always also get
    ///     <see cref="BufferCreateFlags.Storage" />/<see cref="BufferCreateFlags.DeviceAddress" /> from
    ///     <see cref="BufferResourceDescriptor" />, so cases that are read/written purely by shaders (via a
    ///     device address, not a dedicated Vulkan buffer-usage bit) don't need to add anything here.
    /// </summary>
    private BufferCreateFlags GraphBufferUsageToDeviceUsageFlags(GraphBufferUsage usage)
    {
        return usage switch
        {
            GraphBufferUsage.Host => BufferCreateFlags.HostDst,
            GraphBufferUsage.HostThenTransfer => BufferCreateFlags.HostDst | BufferCreateFlags.TransferSrc,
            GraphBufferUsage.HostThenGraphics => BufferCreateFlags.HostDst,
            GraphBufferUsage.HostThenCompute => BufferCreateFlags.HostDst,
            GraphBufferUsage.HostThenIndirect => BufferCreateFlags.HostDst | BufferCreateFlags.Indirect,
            GraphBufferUsage.Transfer => BufferCreateFlags.TransferSrc | BufferCreateFlags.TransferDst,
            GraphBufferUsage.Graphics => BufferCreateFlags.None,
            GraphBufferUsage.Compute => BufferCreateFlags.None,
            GraphBufferUsage.Indirect => BufferCreateFlags.Indirect,
            _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null)
        };
    }

    private ImageCreateFlags DeriveImageUsage(ImageLayout layout)
    {
        return layout switch
        {
            ImageLayout.Undefined => ImageCreateFlags.None,
            ImageLayout.TransferDst or ImageLayout.Present => ImageCreateFlags.TransferDst,
            ImageLayout.TransferSrc => ImageCreateFlags.TransferSrc,
            ImageLayout.ShaderAccess =>
                ImageCreateFlags.TransferSrc | ImageCreateFlags.TransferDst,
            ImageLayout.ColorAttachment => ImageCreateFlags.ColorAttachment,
            ImageLayout.StencilAttachment => ImageCreateFlags.StencilAttachment,
            ImageLayout.DepthAttachment => ImageCreateFlags.DepthAttachment,
            ImageLayout.ShaderReadOnly => ImageCreateFlags.Sampled,
            _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null)
        };
    }

    public void FillResources()
    {
        foreach (var (key, image) in _images)
        {
            if (!image.Usage.HasFlag(ImageCreateFlags.ColorAttachment) &&
                !image.Usage.HasFlag(ImageCreateFlags.StencilAttachment) &&
                !image.Usage.HasFlag(ImageCreateFlags.DepthAttachment) && !image.Usage.HasFlag(ImageCreateFlags.Sampled) &&
                !image.Usage.HasFlag(ImageCreateFlags.Storage))
                // We add this because vulkan images require one of the above at minimum
                image.Usage |= ImageCreateFlags.Sampled;

            IResourceDescriptor descriptor = image.Type switch
            {
                ResourceType.Texture => new TextureResourceDescriptor(image.Extent, image.Format, image.Usage),
                ResourceType.Cubemap => new CubemapResourceDescriptor(image.Extent, image.Format, image.Usage),
                ResourceType.TextureArray => new TextureArrayResourceDescriptor(image.Extent, image.Format,
                    image.Usage, image.Count),
                _ => throw new ArgumentOutOfRangeException(nameof(image.Type), image.Type, null)
            };
            Resources.Add(key, descriptor);
        }

        foreach (var (key, buffer) in _buffers)
            Resources.Add(key, new BufferResourceDescriptor(buffer.Size, buffer.Usage));
    }

    public class Dependency
    {
        public DependencyType Type { get; set; }
        public uint Id { get; set; }
    }

    public class ResourceAction
    {
        public required ResourceOperation Operation { get; set; }
        public required uint PassId { get; set; }
        public required GraphResourceKind Type { get; set; }
        public ImageLayout ImageLayout { get; set; }
        public GraphBufferUsage BufferUsage { get; set; }
        public bool Discard { get; set; }
    }
}