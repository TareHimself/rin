using System.Diagnostics.CodeAnalysis;
using ManagedBass;
using rin.Framework.Core;
using rin.Framework.Core.Extensions;

namespace rin.Framework.Audio.BassAudio;

public class BassAudioModule 
{
    // private App? _app;
    //
    // public App GetApp() => _app ?? throw new Exception("App not initialized");
    //
    // public void Start(App app)
    // {
    //     _app = app;
    //     Bass.Init();
    // }
    //
    // public void Stop(App app)
    // {
    //     Bass.Stop();
    // }
    //
    // public void Update(float deltaSeconds)
    // {
    //     
    // }
    //
    // public ISample SampleFromFile(FileUri path)
    // {
    //     using var stream = GetApp().FileSystem.OpenRead(path);
    //     return SampleFromData(stream);
    // }
    //
    // public ISample SampleFromData(Stream data)
    // {
    //     using var ms = new MemoryStream();
    //     data.CopyTo(ms);
    //     return new BassSample(Bass.SampleLoad(ms.ToArray(), 0, 0, 3000, 0));
    // }
    //
    // public IStream StreamFromFile(FileUri path)
    // {
    //     return new StreamChannel(Bass.CreateStream(StreamSystem.Buffer, 0,MakeFileProcedures(GetApp().FileSystem.OpenRead(path))));
    // }
    //
    // private FileProcedures MakeFileProcedures(Stream stream)
    // {
    //     unsafe
    //     {
    //         return new FileProcedures()
    //         {
    //             Close = { },
    //             Length = (buffer) =>
    //             {
    //                 if (!stream.CanSeek) return 0;
    //
    //                 try
    //                 {
    //                     return stream.Length;
    //                 }
    //                 catch
    //                 {
    //                     return 0;
    //                 }
    //             },
    //             Read = (buffer, length, _) =>
    //             {
    //                 if (!stream.CanRead) return 0;
    //
    //                 try
    //                 {
    //                     return stream.Read(new Span<byte>((void*)buffer, length));
    //                 }
    //                 catch
    //                 {
    //                     return 0;
    //                 }
    //             },
    //             Seek = (offset, user) =>
    //             {
    //                 if (!stream.CanSeek) return false;
    //
    //                 try
    //                 {
    //                     return stream.Seek(offset, SeekOrigin.Begin) == offset;
    //                 }
    //                 catch
    //                 {
    //                     return false;
    //                 }
    //             }
    //         };
    //     }
    // }
    //
    // public IStream StreamFromData(Stream data)
    // {
    //     return new StreamChannel(Bass.CreateStream(StreamSystem.Buffer, 0,MakeFileProcedures(data)));
    // }
}