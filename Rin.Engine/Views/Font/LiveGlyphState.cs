namespace Rin.Engine.Views.Font;

/// <summary>
///     The current state of a glypj
/// </summary>
public enum LiveGlyphState
{
    /// <summary>
    ///     The glyph has not been generated and is not pending
    /// </summary>
    Invalid,

    /// <summary>
    ///     The glyph is being generated
    /// </summary>
    Pending,

    /// <summary>
    ///     The glyph has been generated
    /// </summary>
    Ready
}