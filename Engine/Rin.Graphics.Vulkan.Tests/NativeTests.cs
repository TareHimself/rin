using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Tests;

public class NativeTests
{
    private const IntPtr Allocator = 0x1000;

    [SetUp]
    public void ResetFake()
    {
        FakeNative.Reset();
    }

    private static unsafe void NewBuffer()
    {
        VkBuffer buffer;
        var allocation = IntPtr.Zero;
        Native.NewBuffer(&buffer, ref allocation, 16, Allocator, true, false,
            VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, false, "test");
    }

    private static unsafe void NewImage()
    {
        VkImage image = default;
        var allocation = IntPtr.Zero;
        var createInfo = new VkImageCreateInfo();
        Native.NewImage(ref image, ref allocation, &createInfo, Allocator, "test");
    }

    // The code travels as an int because NUnit needs public test signatures and GraphicsResult is internal.
    private static TestCaseData Export(string export, Native.GraphicsResult code, Action call)
    {
        return new TestCaseData(export, (int)code, call).SetArgDisplayNames(export);
    }

    public static IEnumerable<TestCaseData> FallibleExports()
    {
        yield return Export("createVulkanInstance", Native.GraphicsResult.NoSuitableDevice,
            () => Native.CreateVulkanInstance(0));
        yield return Export("allocatorCreate", Native.GraphicsResult.VulkanError,
            () => Native.CreateAllocator(default, default, default));
        yield return Export("allocatorNewBuffer", Native.GraphicsResult.OutOfDeviceMemory, NewBuffer);
        yield return Export("allocatorNewImage", Native.GraphicsResult.OutOfDeviceMemory, NewImage);
        yield return Export("allocatorCopyToBuffer", Native.GraphicsResult.InvalidArgument,
            () => Native.CopyToBuffer(Allocator, 1, IntPtr.Zero, 0, 0));
        yield return Export("platformWindowCreate", Native.GraphicsResult.WindowCreationFailed,
            () => Native.CreateWindow("test", new Extent2D(1, 1), WindowFlags.None));
        yield return Export("platformWindowCreateSurface", Native.GraphicsResult.SurfaceCreationFailed,
            () => Native.CreateSurface(default, 1));
    }

    [TestCaseSource(nameof(FallibleExports))]
    public void CheckedWrapper_ThrowsNativeExceptionNamingTheExportAndCode(string export, int code, Action call)
    {
        var expected = (Native.GraphicsResult)code;
        FakeNative.FailNext(expected);

        var exception = Assert.Throws<NativeException>(() => call());

        Assert.Multiple(() =>
        {
            Assert.That(exception.Function, Is.EqualTo(export));
            Assert.That(exception.Code, Is.EqualTo(expected));
            Assert.That(exception.Library, Does.EndWith("Rin.Graphics.Vulkan.Native"));
            Assert.That(exception.Message, Does.StartWith(export).And.EndWith($"failed with {expected}"));
        });
    }

    [TestCaseSource(nameof(FallibleExports))]
    public void CheckedWrapper_ReturnsQuietlyOnOk(string export, int code, Action call)
    {
        Assert.DoesNotThrow(() => call());
    }

    [Test]
    public void CreateWindow_ReturnsAHandleTheNativeSideKnows()
    {
        var handle = Native.CreateWindow("test", new Extent2D(640, 480), WindowFlags.Visible);

        Assert.Multiple(() =>
        {
            Assert.That(handle, Is.Not.Zero);
            Assert.That(FakeNative.WindowCount(), Is.EqualTo(1));
            Assert.That(Native.platformWindowGetSize(handle), Is.EqualTo(new Extent2D(640, 480)));
        });
    }

    [Test]
    public unsafe void NewBuffer_WritesTheBufferAndAllocationItWasGiven()
    {
        VkBuffer buffer;
        var allocation = IntPtr.Zero;

        Native.NewBuffer(&buffer, ref allocation, 16, Allocator, true, false,
            VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, false, "test");

        var bufferHandle = buffer.Value;
        var liveAfterCreate = FakeNative.LiveAllocationCount();
        Native.allocatorFreeBuffer(buffer, allocation, Allocator);

        Assert.Multiple(() =>
        {
            Assert.That(bufferHandle, Is.Not.Zero);
            Assert.That(allocation, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That(liveAfterCreate, Is.EqualTo(1));
            Assert.That(FakeNative.LiveAllocationCount(), Is.Zero);
        });
    }

    [Test]
    public unsafe void CreateVulkanInstance_ReturnsTheStructTheNativeSideWrote()
    {
        var expected = new byte[sizeof(Native.VulkanInitResult)];
        for (var i = 0; i < expected.Length; i++) expected[i] = (byte)(i + 1);
        FakeNative.SetInitResult(expected);

        var result = Native.CreateVulkanInstance(0);

        var actual = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref result, 1)).ToArray();
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GraphicsResult_MirrorsResultHpp()
    {
        var header = ReadNativeSource("result.hpp");
        var body = Regex.Match(header, @"enum class GraphicsResult[^{]*\{([^}]*)\}").Groups[1].Value;
        var expected = body.Split(',')
            .Select(entry => Regex.Replace(entry, "//.*", string.Empty).Split('=')[0].Trim())
            .Where(name => name.Length > 0)
            .ToArray();

        Assert.That(Enum.GetNames<Native.GraphicsResult>(), Is.EqualTo(expected));
    }

    [Test]
    public void VulkanInitResult_MirrorsGraphicsHpp()
    {
        var header = ReadNativeSource("graphics.hpp");
        var body = Regex.Match(header, @"struct VulkanInitResult\s*\{([^}]*)\}").Groups[1].Value;
        var expected = body.Split(';')
            .Select(field => Regex.Replace(field, "//.*", string.Empty).Trim())
            .Where(field => field.Length > 0)
            .Select(field => field.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1])
            .ToArray();

        var actual = typeof(Native.VulkanInitResult)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(field => field.Name)
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected));
    }

    private static string ReadNativeSource(string file)
    {
        return File.ReadAllText(Path.Combine(NativeFakeLocator.FindRepoRoot(), "native",
            "Rin.Graphics.Vulkan.Native", "src", file));
    }
}
