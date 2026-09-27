using System.Runtime.InteropServices;
using Rin.Slang.Compiler;

namespace Rin.Slang.Compiler.Tests;

[SetUpFixture]
public class NativeFakeSetup
{
    [OneTimeSetUp]
    public void RedirectRinSlangNativeToFake()
    {
        var fakePath = NativeFakeLocator.Find("Rin.Slang.Native.Fake");
        NativeLibrary.SetDllImportResolver(typeof(ShaderCompiler).Assembly,
            (name, _, _) => name == "Rin.Slang.Native" ? NativeLibrary.Load(fakePath) : IntPtr.Zero);
    }
}
