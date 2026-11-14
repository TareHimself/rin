using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared.Pooling;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Graph;

public class GraphConfig(GraphBuilder builder) : IGraphConfig, IResettable
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

    private readonly CollectionPool<List<ResourceAction>, ResourceAction> _actionLists = new();
    private readonly Dictionary<uint, GraphConfigBuffer> _buffers = [];
    private readonly CollectionPool<List<Dependency>, Dependency> _dependencyLists = new();

    private readonly Dictionary<uint, GraphConfigImage> _images = [];
    private readonly Dictionary<uint, int> _prependedWrites = [];

    public readonly Dictionary<uint, List<Dependency>> PassDependencies = [];
    public readonly Dictionary<uint, List<ResourceAction>> ResourceActions = [];
    public readonly Dictionary<uint, IResourceDescriptor> Resources = [];

    public uint CurrentPassId { get; set; }

    public void Reset()
    {
        _dependencyLists.ReturnAll(PassDependencies);
        _actionLists.ReturnAll(ResourceActions);
        _buffers.Clear();
        _images.Clear();
        _prependedWrites.Clear();
        Resources.Clear();
        CurrentPassId = 0;
    }

    public uint AddExternalImage(ResourceHandle handle, GraphResourceFlags flags = GraphResourceFlags.None,
        Action? onDispose = null)
    {
        if (builder.TryReuseExternalImage(handle, flags, onDispose, out var existingId)) return existingId;
        if (ExternalResourceDescriptors.Make(builder.ExternalResources, handle, onDispose) is not { } descriptor) return 0;
        var resourceId = builder.MakeId();
        Resources.Add(resourceId, descriptor);
        builder.RememberExternalImage(handle, resourceId, flags);
        return resourceId;
    }

    public uint AddExternalBuffer(in DeviceBufferView view, GraphResourceFlags flags = GraphResourceFlags.None,
        Action? onDispose = null)
    {
        if (ExternalResourceDescriptors.MakeBuffer(builder.ExternalResources, view, onDispose) is not { } descriptor) return 0;
        var resourceId = builder.MakeId();
        Resources.Add(resourceId, descriptor);
        builder.RememberExternalBuffer(view.Buffer, resourceId, flags);
        return resourceId;
    }

    public uint CreateTexture(in Extent2D extent, ImageFormat format, ImageLayout layout,
        GraphResourceFlags flags = GraphResourceFlags.None)
    {
        return CreateImage(extent, format, layout, 0, ResourceType.Texture, flags);
    }

    public uint CreateTextureArray(in Extent2D extent, ImageFormat format, uint count, ImageLayout layout,
        GraphResourceFlags flags = GraphResourceFlags.None)
    {
        return CreateImage(extent, format, layout, count, ResourceType.TextureArray, flags);
    }

    public uint CreateCubemap(in Extent2D extent, ImageFormat format, ImageLayout layout,
        GraphResourceFlags flags = GraphResourceFlags.None)
    {
        return CreateImage(extent, format, layout, 0, ResourceType.Cubemap, flags);
    }


    public uint CreateBuffer(ulong size, GraphBufferUsage usage, GraphResourceFlags flags = GraphResourceFlags.None)
    {
        var resourceId = builder.MakeId();
        builder.RememberFlags(resourceId, flags);
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
        AddDependency(DependencyTypeOf(operation), id);
        AddAction(id, new ResourceAction
        {
            Operation = operation,
            PassId = CurrentPassId,
            Type = GraphResourceKind.Image,
            ImageLayout = layout
        });

        ref var image = ref CollectionsMarshal.GetValueRefOrNullRef(_images, id);
        if (!Unsafe.IsNullRef(ref image)) image.Usage |= DeriveImageUsage(layout);

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
        AddDependency(DependencyTypeOf(operation), id);
        AddAction(id, new ResourceAction
        {
            Operation = operation,
            PassId = CurrentPassId,
            Type = GraphResourceKind.Buffer,
            BufferUsage = usage
        });

        ref var buffer = ref CollectionsMarshal.GetValueRefOrNullRef(_buffers, id);
        if (!Unsafe.IsNullRef(ref buffer)) buffer.Usage |= GraphBufferUsageToDeviceUsageFlags(usage);
        return id;
    }

    public uint DependOn(uint passId)
    {
        Debug.Assert(passId != 0, "Invalid Pass Id");
        AddDependency(DependencyType.Pass, passId);
        return passId;
    }

    private static DependencyType DependencyTypeOf(ResourceOperation operation)
    {
        return operation switch
        {
            ResourceOperation.Write => DependencyType.Write,
            ResourceOperation.Read => DependencyType.Read,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    private void AddDependency(DependencyType type, uint id)
    {
        if (!PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
            PassDependencies.Add(CurrentPassId, dependencies = _dependencyLists.Rent());
        dependencies.Add(new Dependency { Type = type, Id = id });
    }

    private void AddAction(uint resourceId, in ResourceAction action)
    {
        if (!ResourceActions.TryGetValue(resourceId, out var actions))
            ResourceActions.Add(resourceId, actions = _actionLists.Rent());
        actions.Add(action);
    }


    internal void PrependTextureWrite(uint id, bool discard)
    {
        UseTexture(id, ImageLayout.TransferDst, ResourceOperation.Write);
        MoveLastActionToPrepended(id, discard);
    }

    internal void PrependBufferWrite(uint id)
    {
        UseBuffer(id, GraphBufferUsage.Transfer, ResourceOperation.Write);
        MoveLastActionToPrepended(id);
    }

    internal void PrependTextureRead(uint id)
    {
        UseTexture(id, ImageLayout.TransferSrc, ResourceOperation.Read);
        MoveLastActionToPrepended(id);
    }

    internal void PrependBufferRead(uint id)
    {
        UseBuffer(id, GraphBufferUsage.Transfer, ResourceOperation.Read);
        MoveLastActionToPrepended(id);
    }

    private void MoveLastActionToPrepended(uint id, bool discard = false)
    {
        var actions = ResourceActions[id];
        var action = actions[^1];
        action.Discard = discard;
        actions.RemoveAt(actions.Count - 1);
        var index = _prependedWrites.GetValueOrDefault(id);
        actions.Insert(index, action);
        _prependedWrites[id] = index + 1;
    }

    private uint CreateImage(in Extent2D extent, ImageFormat format, ImageLayout layout, uint count, ResourceType type,
        GraphResourceFlags resourceFlags)
    {
        if (extent.Width == 0 || extent.Height == 0)
            throw new ArgumentOutOfRangeException(nameof(extent), extent, "Image dimensions must be greater than zero");
        var flags = format switch
        {
            ImageFormat.Depth => ImageCreateFlags.DepthAttachment,
            ImageFormat.Stencil => ImageCreateFlags.StencilAttachment,
            _ => ImageCreateFlags.None
        };
        var resourceId = builder.MakeId();
        builder.RememberFlags(resourceId, resourceFlags);
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
    ///     instead. Every buffer is a storage buffer, so cases that are read/written purely by shaders don't
    ///     need to add anything here.
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
            const ImageCreateFlags attachable = ImageCreateFlags.ColorAttachment | ImageCreateFlags.StencilAttachment |
                                                ImageCreateFlags.DepthAttachment | ImageCreateFlags.Sampled |
                                                ImageCreateFlags.Storage;
            var usage = image.Usage;
            // We add this because vulkan images require one of the above at minimum
            if ((usage & attachable) == 0) usage |= ImageCreateFlags.Sampled;

            IResourceDescriptor descriptor = image.Type switch
            {
                ResourceType.Texture => new TextureResourceDescriptor(image.Extent, image.Format, usage),
                ResourceType.Cubemap => new CubemapResourceDescriptor(image.Extent, image.Format, usage),
                ResourceType.TextureArray => new TextureArrayResourceDescriptor(image.Extent, image.Format,
                    usage, image.Count),
                _ => throw new ArgumentOutOfRangeException(nameof(image.Type), image.Type, null)
            };
            Resources.Add(key, descriptor);
        }

        foreach (var (key, buffer) in _buffers)
            Resources.Add(key, new BufferResourceDescriptor(buffer.Size, buffer.Usage));
    }

    public struct Dependency
    {
        public DependencyType Type { get; set; }
        public uint Id { get; set; }
    }

    public struct ResourceAction
    {
        public required ResourceOperation Operation { get; set; }
        public required uint PassId { get; set; }
        public required GraphResourceKind Type { get; set; }
        public ImageLayout ImageLayout { get; set; }
        public GraphBufferUsage BufferUsage { get; set; }
        public bool Discard { get; set; }
    }
}