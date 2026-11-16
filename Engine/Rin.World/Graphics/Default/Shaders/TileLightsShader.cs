using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

/// <summary>
///     Fills the tile bitmasks: one thread per (tile, 32-item word), each writing its own word, so no atomics. Every
///     kind of item is tested by its sphere.
/// </summary>
[Shader]
public partial class TileLightsShader : Shader
{
    public struct PushConstants
    {
        public DeviceSpan<LightGridInfo> Grid;
    }

    [Push] [UsedImplicitly] protected PushConstants Push;

    [Compute(64, 1, 1)]
    public void Compute(ComputeIn input)
    {
        var grid = Push.Grid[0];
        var index = input.ThreadId;
        if (index >= grid.TilesX * grid.TilesY * grid.WordCount) return;

        var word = index % grid.WordCount;
        var tile = index / grid.WordCount;
        var slopes = LightGrid.TileSlopes(tile % grid.TilesX, tile / grid.TilesX, grid.Width, grid.Height,
            grid.TanHalfFovY, grid.Aspect);

        var mask = 0u;
        for (var bit = 0u; bit < 32u; bit++)
        {
            var itemIndex = word * 32u + bit;
            if (itemIndex >= (uint)grid.ItemCount) break;

            var item = grid.Items[itemIndex];
            var center = Vector4.Transform(new Vector4(item.Center, 1f), grid.View);
            if (LightGrid.SphereTouchesTile(new Vector3(center.X, center.Y, center.Z), item.Radius, slopes))
                mask |= 1u << (int)bit;
        }

        grid.TileMasks[index] = mask;
    }
}
