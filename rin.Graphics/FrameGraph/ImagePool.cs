using TerraFX.Interop.Vulkan;

namespace rin.Graphics.FrameGraph;

public class ImagePool(WindowRenderer renderer) : IImagePool
{
    struct PooledImage
    {
        public required DeviceImage Image;
        public required ulong LastUsed;
    }
    
    private readonly Dictionary<string, PooledImage> _imagePool = [];
    private ulong _currentFrame = 0;
    
    public void Dispose()
    {
        foreach (var (_,image) in _imagePool)
        {
            image.Image.Dispose();
        }
    }

    public DeviceImage GetOrCreateImage(ImageResourceDescriptor descriptor, string id)
    {
        var cacheId = $"{descriptor.GetHashCode()}-{id}";

        {
            if (_imagePool.TryGetValue(cacheId, out var cached))
            {
                if (cached.LastUsed != _currentFrame)
                {
                    cached.LastUsed = _currentFrame;
                    _imagePool[cacheId] = cached;
                }

                return cached.Image;
            }
        }
        
        Console.WriteLine("FrameGraph :: Image :: Allocate :: {0}",cacheId);
        var image = SGraphicsModule.Get().CreateImage(new VkExtent3D()
        {
            width = descriptor.Width,
            height = descriptor.Height,
            depth = 1
        },descriptor.Format,descriptor.Flags,debugName: "Frame Graph Image");
        
        _imagePool.Add(cacheId,new PooledImage()
        {
            Image = image,
            LastUsed = _currentFrame
        });

        return image;
    }

    public void OnFrameStart(ulong newFrame)
    {
        var entries = _imagePool.ToArray();
        var frameCount = renderer.GetFrameCount();
        foreach (var (key,data) in entries)
        {
            if (data.LastUsed + frameCount < newFrame)
            {
                data.Image.Dispose();
                Console.WriteLine("FrameGraph :: Image :: Free :: {0}",key);
                _imagePool.Remove(key);
            }
        }
        _currentFrame = newFrame;
    }
}