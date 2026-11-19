namespace Rin.World.Graphics;

/// <summary>
///     Projection parameters of a camera entity. Its position and rotation come from <see cref="Transforms.WorldTransform" />.
/// </summary>
public struct Camera() : IComponentData
{
    /// <summary>
    ///     Vertical field of view, in degrees.
    /// </summary>
    public float FieldOfView = 90.0f;

    public float NearClipPlane = 0.01f;

    /// <summary>
    ///     How far the view frustum reaches for culling. The projection is infinite reversed-Z, so it does not clip.
    /// </summary>
    public float FarClipPlane = 50.0f;

    /// <summary>
    ///     Exposure in stops at ISO 100. Higher lets in less light: about 15 for direct sun, 5 to 9 indoors.
    /// </summary>
    public float Ev100 = 15.0f;

    // 1.2 is the lens and sensor constant from Lagarde and de Rousiers, "Moving Frostbite to PBR".
    public readonly float Exposure => 1.0f / (1.2f * MathF.Pow(2.0f, Ev100));

    public ToneMapCurve ToneMapCurve = ToneMapCurve.AgX;
}
