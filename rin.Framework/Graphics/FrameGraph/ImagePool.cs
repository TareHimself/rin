using rin.Framework.Core.Extensions;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class ImagePool(WindowRenderer renderer) : IImagePool
{

    private class AllocatedImage(IDeviceImage image, ulong lastUsed) : IDisposable
    {
        public readonly IDeviceImage Image = image;
        public ulong LastUsed = lastUsed;
        public readonly HashSet<Frame> Uses = [];

        public void Dispose()
        {
            Image.Dispose();
        }
    }
    private readonly struct PooledImage : IDeviceImage
    {
        private readonly AllocatedImage _image;
        private readonly Frame _frame;

        public PooledImage(AllocatedImage image,Frame frame)
        {
            _image = image;
            _frame = frame;
            _image.Uses.Add(_frame);
        }
        
        public void Dispose()
        {
            _image.Uses.Remove(_frame);
        }
        
        public ImageFormat Format => _image.Image.Format;
        public VkExtent3D Extent => _image.Image.Extent;
        public VkImage NativeImage => _image.Image.NativeImage;

        public VkImageView NativeView
        {
            get => _image.Image.NativeView;
            set {}
        }
    }
    
    
    
    private readonly Dictionary<int, HashSet<AllocatedImage>> _imagePool = [];
    private ulong _currentFrame = 0;
    
    public void Dispose()
    {
        foreach (var (_,images) in _imagePool)
        {
            foreach (var image in images)
            {
                image.Dispose();
            }
        }
    }

    public IDeviceImage CreateImage(ImageResourceDescriptor descriptor,Frame frame)
    {
        var cacheId = descriptor.GetHashCode();

        {
            if (_imagePool.TryGetValue(cacheId, out var images))
            {
                foreach (var image in images)
                {
                    if (!image.Uses.Contains(frame))
                    {
                        image.LastUsed = _currentFrame;
                        return new PooledImage(image, frame);
                    }
                }
            }
        }

        {
            if (!_imagePool.ContainsKey(cacheId))
            {
                _imagePool.Add(cacheId,[]);
            }
            
            //Console.WriteLine("FrameGraph :: Image :: Allocate :: {0}",cacheId);
            var image = SGraphicsModule.Get().CreateImage(new VkExtent3D()
            {
                width = descriptor.Width,
                height = descriptor.Height,
                depth = 1
            },descriptor.Format,descriptor.Flags,debugName: "Frame Graph Image");
            
            Console.WriteLine("Allocating new frame graph image: {0}",descriptor.ToString());

            var allocated = new AllocatedImage(image, _currentFrame);
            _imagePool[cacheId].Add(allocated);
            
            return new PooledImage(allocated, frame);
        }
    }

    public void OnFrameStart(ulong newFrame)
    {
        var entries = _imagePool.ToArray();
        var frameCount = renderer.GetFrameCount();
        foreach (var (key,images) in entries)
        {
            images.RemoveWhere((image) =>
            {
                if (image.Uses.NotEmpty())
                {
                    return false;
                }

                if (image.LastUsed + frameCount < newFrame)
                {
                    image.Dispose();
                    return true;
                }

                return false;
            });
        }
        _currentFrame = newFrame;
    }
}