using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Shared.Threading;

namespace rin.Examples.ViewsTest;

public class ImageLoader
{
    private static readonly HttpClient Client = new();

    private readonly BackgroundTaskQueue _taskQueue = new()
    {
        Name = "Image Loader Queue"
    };

    public void Load(string source, Action<ResourceHandle> onLoad)
    {
        _taskQueue.Enqueue(() =>
        {
            ResourceHandle imageHandle;
            if (source.StartsWith("http"))
            {
                using var resp = Client.Send(new HttpRequestMessage(HttpMethod.Get, source));
                resp.EnsureSuccessStatusCode();
                using var image = HostImage.Create(resp.Content.ReadAsStream());
                image.CreateTexture(out imageHandle).Wait();
                
            }
            else
            {
                using var resp = Client.Send(new HttpRequestMessage(HttpMethod.Get, source));
                resp.EnsureSuccessStatusCode();
                using var data = File.OpenRead(source);
                using var image = HostImage.Create(data);
                image.CreateTexture(out imageHandle).Wait();
            }
            IApplication.Get().MainDispatcher.Enqueue(static state => state.onLoad(state.imageHandle),(onLoad,imageHandle));
        });
    }
}