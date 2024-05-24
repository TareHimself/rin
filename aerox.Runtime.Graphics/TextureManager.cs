using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics;

public class TextureManager : Disposable
{
    private readonly SortedDictionary<int, Texture> _textures = new();
    private readonly Mutex _texturesMutex = new();
    
    public TextureHandle CreateTexture(byte[] data, VkExtent3D size, ImageFormat format, ImageFilter filter, ImageTiling tiling, bool mipMapped = true,
        string debugName = "Texture")
    {
        lock (_texturesMutex)
        {
            var texture = new Texture(data, size, format, filter, tiling, mipMapped, debugName);
            var id = 0;
            if (_textures.Count == 0)
            {
                _textures.Add(id,texture);
            }
            else
            {
                id = _textures.Last().Key;
                _textures.Add(id,texture);
            }

            return new TextureHandle()
            {
                TextureId = id
            };
        }
    }

    public Texture? TextureFromHandle(TextureHandle handle)
    {
        lock (_texturesMutex)
        {
            return _textures.GetValueOrDefault(handle.TextureId);
        }
    }
    protected override void OnDispose(bool isManual)
    {
        lock (_texturesMutex)
        {
            foreach (var texture in _textures)
            {
                texture.Value.Dispose();
            }
        }
    }
}