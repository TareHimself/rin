namespace Rin.Framework.Audio;

/// <summary>
///     A stream that requires data to be pushed into it
/// </summary>
public interface IPushStream : IActiveAudio
{
    public void Push(in ReadOnlySpan<byte> data);
}