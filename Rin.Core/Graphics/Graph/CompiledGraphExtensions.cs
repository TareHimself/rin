namespace Rin.Core.Graphics.Graph;

public static class CompiledGraphExtensions
{
    extension(ICompiledGraph graph)
    {
        public ResourceHandle GetImageOrException(uint imageId)
        {
            return imageId <= 0 ? throw new NullReferenceException() : graph.GetImage(imageId);
        }

        public DeviceBufferView GetBufferOrNull(uint bufferId)
        {
            return bufferId <= 0 ? new DeviceBufferView() : graph.GetBuffer(bufferId);
        }

        public DeviceBufferView GetBufferOrException(uint bufferId)
        {
            return bufferId <= 0 ? throw new NullReferenceException() : graph.GetBuffer(bufferId);
        }
    }
}