namespace Rin.World.Benchmarks;

public struct Position : IComponentData
{
    public float X, Y, Z;
}

public struct Velocity : IComponentData
{
    public float X, Y, Z;
}

public struct Health : IComponentData
{
    public int Value;
}

public struct Mass : IComponentData
{
    public float Value;
}

public struct Marker : IComponentData
{
    public int Value;
}
