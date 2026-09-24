using System.Buffers.Binary;
using System.Security.Cryptography;
using Rin.Core.Graphics;

namespace Examples.Common;

public sealed class TextureCache : IDisposable
{
    private readonly record struct Key(UInt128 PixelHash, Extent2D Extent, HostImageFormat Format, bool Mips);

    private readonly Dictionary<Key, (ResourceHandle Handle, Task<ResourceHandle> Task)> _textures = [];
    private readonly Lock _sync = new();

    public async Task<ResourceHandle> Load(string path, bool mips = false)
    {
        using var image = await Task.Run(() => HostImage.Create(File.OpenRead(path)));
        return await Get(image, mips);
    }

    public Task<ResourceHandle> Get(IHostImage image, bool mips = false)
    {
        var key = new Key(HashPixels(image), image.Extent, image.Format, mips);
        lock (_sync)
        {
            if (_textures.TryGetValue(key, out var existing)) return existing.Task;

            var task = image.CreateTexture(out var handle, mips: mips);
            _textures.Add(key, (handle, task));
            return task;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            IGraphicsModule.Get().FreeResourceHandles(_textures.Values.Select(c => c.Handle).ToArray());
            _textures.Clear();
        }
    }

    private static UInt128 HashPixels(IHostImage image)
    {
        using var pixels = new MemoryStream();
        image.SaveRaw(pixels);
        var digest = SHA256.HashData(pixels.GetBuffer().AsSpan(0, (int)pixels.Length));
        return BinaryPrimitives.ReadUInt128LittleEndian(digest);
    }
}
