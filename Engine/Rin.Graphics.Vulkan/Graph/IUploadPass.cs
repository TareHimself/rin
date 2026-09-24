using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

internal interface IUploadPass : IPass, IDisposable
{
    void OnSubmitted();
}
