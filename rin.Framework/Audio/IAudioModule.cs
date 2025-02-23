using rin.Framework.Core;

namespace rin.Framework.Audio;

public interface IAudioModule : IAppModule, IUpdatable
{
    public ISample SampleFromFile(FileUri path);
    public ISample SampleFromData(Stream data);
    public IStream StreamFromFile(FileUri path);
    public IStream StreamFromData(Stream data);
}