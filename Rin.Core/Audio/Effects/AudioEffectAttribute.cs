namespace Rin.Core.Audio.Effects;

/// <summary>
/// Marks a <c>partial</c> struct or class as an audio effect. The source generator emits an
/// <c>UnmanagedCallersOnly</c> processing shim plus a generated <c>Descriptor</c> static field
/// (an <see cref="global::Rin.Core.Audio.Effects.IAudioEffectDescriptor{TParams}"/>) that you pass to
/// <see cref="global::Rin.Core.Audio.ISupportsAudioEffects.AddEffect{TParams}"/> to attach the effect to a bus.
/// </summary>
/// <remarks>
/// <para>
/// The type must declare exactly one static method named <c>Process</c>. There is no interface to
/// implement - the generator recognizes parameters purely by name (order doesn't matter):
/// </para>
/// <list type="bullet">
/// <item><c>ReadOnlySpan&lt;float&gt; input</c> - required, the incoming audio block.</item>
/// <item><c>Span&lt;float&gt; output</c> - required, write the processed audio here.</item>
/// <item><c>AudioEffectContext ctx</c> (by value or <c>in</c>) - optional, gives SampleRate/Channels/Time.</item>
/// <item><c>in TParams parameters</c> where <c>TParams : unmanaged</c> - optional, your tunable knobs.
/// Live-updatable at runtime through the <c>IEffectController&lt;TParams&gt;.Parameters</c> setter
/// without removing/re-adding the effect.</item>
/// <item><c>ref TState state</c> where <c>TState : unmanaged</c> - optional, persistent per-instance
/// scratch data (e.g. filter delay lines). Zero-initialized when the effect is added.</item>
/// </list>
/// <para>
/// <c>Process</c> must be static; the type must be <c>partial</c>. Omitting <c>parameters</c> or
/// <c>state</c> is fine - the generated descriptor substitutes a zero-size placeholder type for
/// whichever one you don't declare.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AudioEffectAttribute : Attribute
{
}
