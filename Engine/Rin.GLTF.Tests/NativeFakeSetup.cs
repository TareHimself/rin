using System.Runtime.InteropServices;
using Rin.Core.Shared.Buffers;

namespace Rin.GLTF.Tests;

[SetUpFixture]
public class NativeFakeSetup
{
    [OneTimeSetUp]
    public void RedirectRinNativeToFake()
    {
        var fakePath = NativeFakeLocator.Find("Rin.Native.Fake");
        NativeLibrary.SetDllImportResolver(typeof(Buffer<byte>).Assembly,
            (name, _, _) => name == "Rin.Native" ? NativeLibrary.Load(fakePath) : IntPtr.Zero);
    }
}
