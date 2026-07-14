using System.Runtime.CompilerServices;
using Rin.Core.Shared.Buffers;

namespace Rin.Core.Audio.Effects;

/// <summary>
/// Handle to an effect instance attached to a bus, returned by
/// <see cref="global::Rin.Core.Audio.ISupportsAudioEffects.AddEffect{TParams}"/>. Disposing it detaches
/// and releases the effect.
/// </summary>
public interface IEffectController : IDisposable
{
   public ulong Id { get; }
}

/// <summary>
/// Adds a settable <see cref="Parameters"/> struct backed directly by the unmanaged parameter
/// buffer the audio thread reads from - safe to call from any thread, and takes effect starting
/// with the next processed block.
/// </summary>
public interface IEffectController<TParams> : IEffectController where TParams : unmanaged
{
   public TParams Parameters { get; set; }
}