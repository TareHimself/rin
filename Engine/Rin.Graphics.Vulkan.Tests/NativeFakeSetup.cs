using System.Runtime.InteropServices;

namespace Rin.Graphics.Vulkan.Tests;

[SetUpFixture]
public class NativeFakeSetup
{
    [OneTimeSetUp]
    public void RedirectVulkanNativeToFake()
    {
        var fakePath = NativeFakeLocator.Find("Rin.Graphics.Vulkan.Native.Fake");
        NativeLibrary.SetDllImportResolver(typeof(VulkanGraphicsModule).Assembly,
            (name, _, _) => name == "Rin.Graphics.Vulkan.Native" ? NativeLibrary.Load(fakePath) : IntPtr.Zero);
        NativeLibrary.SetDllImportResolver(typeof(NativeFakeSetup).Assembly,
            (name, _, _) => name == FakeNative.DllName ? NativeLibrary.Load(fakePath) : IntPtr.Zero);
    }
}
