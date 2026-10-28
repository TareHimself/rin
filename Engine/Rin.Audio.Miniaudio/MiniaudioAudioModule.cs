using Rin.Core;
using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio;

public class MiniaudioAudioModule : IAudioModule
{
    private readonly IMiniaudioEngine _engine;

    public MiniaudioAudioModule() : this(new MiniaudioEngine())
    {
    }

    internal MiniaudioAudioModule(IMiniaudioEngine engine)
    {
        _engine = engine;
    }

    public IAudioGroup MasterAudioGroup { get; private set; } = null!;

    public void Start(IApplication app)
    {
        _engine.Init();
        MasterAudioGroup = new MiniaudioAudioGroup(_engine, _engine.MasterMixer, isMaster: true);
    }

    public void Stop(IApplication app)
    {
        _engine.Shutdown();
    }

    public void Update(float deltaTime)
    {
    }

    public float GetVolume() => _engine.Volume;

    public void SetVolume(float volume) => _engine.Volume = volume;

    public IAudioSample MakeSample(string filePath) =>
        new MiniaudioAudioSample(_engine, _engine.SampleLoad(filePath));

    public IAudioSample MakeSample(Stream fileStream) =>
        new MiniaudioAudioSample(_engine, _engine.SampleLoad(ReadAll(fileStream)));

    public IAudioSample MakeStream(string filePath) =>
        new MiniaudioAudioSample(_engine, _engine.StreamOpen(filePath));

    public IAudioSample MakeStream(Stream fileStream) =>
        new MiniaudioAudioSample(_engine, _engine.StreamOpen(ReadAll(fileStream)));

    public IAudioGroup CreateGroup() => MasterAudioGroup.CreateSubGroup();

    // public IDirectionalAudioGroup CreateDirectionalBus(IAudioGroup? parent = null)
    // {
    //     var parentId = parent is MiniaudioAudioGroup b ? b.Id
    //         : parent is MiniaudioDirectionalAudioMixer d ? d.Id
    //         : _engine.MasterMixer;
    //     return new MiniaudioDirectionalAudioMixer(Native.audioCreateScene(parentId));
    // }

    private static byte[] ReadAll(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
