namespace rin.Framework.Scene.Graphics;

public class DrawCommands
{
    private readonly List<DeviceLight> _lights = [];

    public DrawCommands AddLight(DeviceLight light)
    {
        _lights.Add(light);
        return this;
    }
}