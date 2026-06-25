using System.Runtime.CompilerServices;
using Rin.Core.Shared.Buffers;

namespace Rin.Core.Audio.Effects;

public interface IEffectController : IDisposable
{
   public ulong Id { get; }
}

public interface IEffectController<TParams> : IEffectController where TParams : unmanaged
{
   public TParams Parameters { get; set; }
}