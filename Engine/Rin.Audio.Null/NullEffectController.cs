using Rin.Core.Audio.Effects;

namespace Rin.Audio.Null;

internal sealed class NullEffectController<TParams>(ulong id, TParams parameters) : IEffectController<TParams>
    where TParams : unmanaged
{
    public ulong Id => id;
    public TParams Parameters { get; set; } = parameters;

    public void Dispose()
    {
    }
}
