using JetBrains.Annotations;

namespace rin.Framework.Scene.Graphics;

public class DrawCommands
{
    [PublicAPI]
    public readonly List<DeviceLight> Lights = [];
    
    [PublicAPI]
    public readonly List<ICommand> Commands = [];

    public DrawCommands AddLight(DeviceLight light)
    {
        Lights.Add(light);
        return this;
    }

    public DrawCommands AddCommand(ICommand command)
    {
        Commands.Add(command);
        return this;
    }
}