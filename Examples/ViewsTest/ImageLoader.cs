using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Shared.Threading;
using Rin.Core.Views.Content;

namespace ViewsTest;

public class ImageLoader
{
    private static readonly HttpClient Client = new();

    private readonly BackgroundTaskQueue _taskQueue = new()
    {
        Name = "Image Loader Queue"
    };

    public void Load(string source, Action<ImageInfo> onLoad)
    {
        _taskQueue.Enqueue(() =>
        {
            ResourceHandle imageHandle;
            Extent2D extent;
            if (source.StartsWith("http"))
            {
                using var resp = Client.Send(new HttpRequestMessage(HttpMethod.Get, source));
                resp.EnsureSuccessStatusCode();
                using var image = HostImage.Create(resp.Content.ReadAsStream());
                extent = image.Extent;
                image.CreateTexture(out imageHandle).Wait();
                
            }
            else
            {
                using var resp = Client.Send(new HttpRequestMessage(HttpMethod.Get, source));
                resp.EnsureSuccessStatusCode();
                using var data = File.OpenRead(source);
                using var image = HostImage.Create(data);
                extent = image.Extent;
                image.CreateTexture(out imageHandle).Wait();
            }
            IApplication.Get().MainDispatcher.Enqueue(static state => state.onLoad(new ImageInfo(state.imageHandle,state.extent)),(onLoad,imageHandle,extent));
        });
    }
}