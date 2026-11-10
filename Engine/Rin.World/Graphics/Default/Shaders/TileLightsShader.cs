using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

/// <summary>
///     Fills the tile bitmasks: one thread per (tile, 32-light word), each writing its own word, so no atomics.
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
            var lightIndex = word * 32u + bit;
            if (lightIndex >= (uint)grid.PointLightCount) break;

            var light = grid.PointLights[lightIndex];
            var center = Vector4.Transform(new Vector4(light.Location, 1f), grid.View);
            if (LightGrid.SphereTouchesTile(new Vector3(center.X, center.Y, center.Z), light.Radius, slopes))
                mask |= 1u << (int)bit;
        }

        grid.TileMasks[index] = mask;
    }
}
