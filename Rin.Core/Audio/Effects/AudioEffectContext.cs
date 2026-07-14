namespace Rin.Core.Audio.Effects;

/// <summary>
/// Per-block context passed to an effect's <c>Process</c> method when it declares a <c>ctx</c>
/// parameter (by value or <c>in</c>). Values reflect the bus's audio engine at the moment the block
/// is processed, so <see cref="SampleRate"/>/<see cref="Channels"/> should be treated as possibly
/// changing between calls (e.g. on device changes) rather than cached once.
/// </summary>
public struct AudioEffectContext
{
    /// <summary>Running time in seconds since the owning bus's effect chain started processing.</summary>
    public float Time;

    /// <summary>Sample rate, in Hz, of the audio block currently being processed.</summary>
    public int SampleRate;

    /// <summary>Number of interleaved channels in <c>input</c>/<c>output</c>.</summary>
    public int Channels;
}