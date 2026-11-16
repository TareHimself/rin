using System.Numerics;
using JetBrains.Annotations;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

public enum ClusterItemType
{
    PointLight,
    EnvironmentVolume
}

/// <summary>
///     One entry of a view's sorted item list: the sphere the grid culls with, and which entry of its kind's data
///     array it stands for.
/// </summary>
[ShadeExport]
[NoReorder]
public struct ClusterItem
{
    public Vector3 Center;
    public float Radius;
    public ClusterItemType Type;
    public uint Index;
}
