using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Tests;

public class VulkanExceptionTests
{
    [TestCase(VkResult.VK_SUCCESS)]
    [TestCase(VkResult.VK_NOT_READY)]
    [TestCase(VkResult.VK_INCOMPLETE)]
    [TestCase(VkResult.VK_SUBOPTIMAL_KHR)]
    public void ThrowIfFailed_LetsSuccessCodesThrough(VkResult result)
    {
        Assert.DoesNotThrow(() => result.ThrowIfFailed("vkCall"));
    }

    [TestCase(VkResult.VK_ERROR_DEVICE_LOST)]
    [TestCase(VkResult.VK_ERROR_OUT_OF_DATE_KHR)]
    [TestCase(VkResult.VK_ERROR_OUT_OF_DEVICE_MEMORY)]
    public void ThrowIfFailed_ThrowsVulkanExceptionNamingTheCall(VkResult result)
    {
        var exception = Assert.Throws<VulkanException>(() => result.ThrowIfFailed("vkCall"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Result, Is.EqualTo(result));
            Assert.That(exception.Call, Is.EqualTo("vkCall"));
            Assert.That(exception.Message, Is.EqualTo($"vkCall failed with {result}"));
        });
    }
}
