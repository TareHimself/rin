using Rin.Core.Graphics;

namespace Rin.World.Graphics.Surfaces;

/// <summary>
///     Collects the textures a material reports from <see cref="ISurfaceMaterial{TSelf}.GetTextures" />. Unset handles
///     (id 0) are skipped.
/// </summary>
public ref struct TextureList(Span<DeviceHandle> buffer)
{
    private readonly Span<DeviceHandle> _buffer = buffer;

    public int Count { get; private set; }

    public void Add(DeviceHandle texture)
    {
        if (texture.Id == 0) return;
        if (Count == _buffer.Length)
            throw new InvalidOperationException($"A material reported more than {_buffer.Length} textures.");
        _buffer[Count++] = texture;
    }

    public readonly ReadOnlySpan<DeviceHandle> AsSpan() => _buffer[..Count];
}
