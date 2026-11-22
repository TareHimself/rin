using Rin.World;

namespace Examples.Tests;

internal enum SensorMode
{
    Cone,
    Sphere
}

internal struct SensorRange
{
    public SensorMode Mode;
    public float Reach;
    public bool IsOn;
}

internal struct Sensor : IComponentData
{
    public int Channel;
    public SensorRange Range;
}
