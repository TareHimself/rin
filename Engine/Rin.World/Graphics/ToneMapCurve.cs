namespace Rin.World.Graphics;

/// <summary>
///     How scene brightness, which has no upper limit, is squeezed into what a display can show.
/// </summary>
public enum ToneMapCurve
{
    /// <summary>
    ///     Bright colours fade to white along their own hue, with a long, gradual highlight range.
    /// </summary>
    AgX,

    /// <summary>
    ///     Keeps colours close to how they were authored and rolls only the highlights off towards white.
    /// </summary>
    Neutral,

    /// <summary>
    ///     A contrasty, filmic curve. Very saturated bright colours shift in hue.
    /// </summary>
    Aces
}
