using System.Runtime.InteropServices;
using Rin.Slang.Compiler;
using Rin.TestSupport;

namespace Rin.Slang.Compiler.Tests;

[SetUpFixture]
public class NativeFakeSetup
{
    [OneTimeSetUp]
    public void RedirectRinSlangNativeToFake()
    {
        var fakePath = RepoFiles.NativeFake("Rin.Slang.Native.Fake");
        NativeLibrary.SetDllImportResolver(typeof(ShaderCompiler).Assembly,
            (name, _, _) => name == "Rin.Slang.Native" ? NativeLibrary.Load(fakePath) : IntPtr.Zero);
    }
}
