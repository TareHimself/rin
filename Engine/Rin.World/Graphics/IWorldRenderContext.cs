namespace Rin.World.Graphics;

public interface IWorldRenderContext
{
    public uint GetOutputImageId();

    /// <summary>0=color/roughness, 1=location/metallic, 2=normal/specular, 3=emissive; 0 if unsupported.</summary>
    public uint GetGBufferImageId(int index)
    {
        return 0;
    }

    public LightInfo[] GetLights()
    {
        return [];
    }
}