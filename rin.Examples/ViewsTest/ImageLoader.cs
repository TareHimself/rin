using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Images;
using Rin.Core.Shared.Threading;

namespace rin.Examples.ViewsTest;

public class ImageLoader
{
    private static readonly HttpClient Client = new();
    private readonly BackgroundTaskQueue _taskQueue = new();

    public void Load(string source, Action<ImageHandle> onLoad)
    {
        _taskQueue.Enqueue(() =>
        {
            if (source.StartsWith("http"))
            {
                using var resp = Client.Send(new HttpRequestMessage(HttpMethod.Get, source));
                resp.EnsureSuccessStatusCode();
                using var image = HostImage.Create(resp.Content.ReadAsStream());
                image.CreateTexture(out var imageHandle).Wait();
                IApplication.Get().MainDispatcher.Enqueue(() => { onLoad(imageHandle); });
            }
            else
            {
                using var resp = Client.Send(new HttpRequestMessage(HttpMethod.Get, source));
                resp.EnsureSuccessStatusCode();
                using var data = File.OpenRead(source);
                using var image = HostImage.Create(data);
                image.CreateTexture(out var imageHandle).Wait();
                IApplication.Get().MainDispatcher.Enqueue(() => { onLoad(imageHandle); });
            }
        });
    }
}