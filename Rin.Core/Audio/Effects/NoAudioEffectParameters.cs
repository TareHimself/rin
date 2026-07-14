namespace Rin.Core.Audio.Effects;

/// <summary>
/// Zero-size placeholder the source generator substitutes for an effect's <c>Process</c> method's
/// <c>parameters</c> argument. Carries no data - you never construct or reference this directly.
/// </summary>
public readonly struct NoAudioEffectParameters;