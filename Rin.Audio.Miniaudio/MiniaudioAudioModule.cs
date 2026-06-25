using Rin.Core;
using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio;

public class MiniaudioAudioModule : IAudioModule
{
    public IAudioGroup MasterAudioGroup { get; private set; } = null!;

    public void Start(IApplication app)
    {
        if (Native.audioInit() == 0)
            throw new InvalidOperationException("Failed to initialize miniaudio engine");
        MasterAudioGroup = new MiniaudioAudioGroup(Native.audioGetMasterMixerId(), isMaster: true);
    }

    public void Stop(IApplication app)
    {
        Native.audioShutdown();
    }

    public void Update(float deltaTime)
    {
    }

    public float GetVolume() => Native.audioGetVolume();

    public void SetVolume(float volume) => Native.audioSetVolume(volume);

    public IAudioSample MakeSample(string filePath) =>
        MiniaudioAudioSample.FromFile(filePath);

    public IAudioSample MakeSample(Stream fileStream) =>
        MiniaudioAudioSample.FromMemory(ReadAll(fileStream));

    public IAudioSample MakeStream(string filePath) =>
        MiniaudioAudioSample.StreamFromFile(filePath);

    public IAudioSample MakeStream(Stream fileStream) =>
        MiniaudioAudioSample.StreamFromMemory(ReadAll(fileStream));

    public IAudioGroup CreateGroup() => MasterAudioGroup.CreateSubGroup();

    // public IDirectionalAudioGroup CreateDirectionalBus(IAudioGroup? parent = null)
    // {
    //     var parentId = parent is MiniaudioAudioGroup b ? b.Id
    //         : parent is MiniaudioDirectionalAudioMixer d ? d.Id
    //         : Native.audioGetMasterMixerId();
    //     return new MiniaudioDirectionalAudioMixer(Native.audioCreateScene(parentId));
    // }

    private static ReadOnlySpan<byte> ReadAll(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
