namespace Rin.World.Graphics;

/// <summary>
///     What a view draws: the lit scene, or one input of its lighting for debugging.
/// </summary>
public enum RenderChannel
{
    Scene,
    Color,
    Location,
    Normal,
    RoughnessMetallicSpecular,
    Emissive,
    Radiance,
    LightCount
}
