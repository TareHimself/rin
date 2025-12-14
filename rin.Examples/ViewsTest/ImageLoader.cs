using Rin.Framework;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Shared.Threading;

namespace rin.Examples.ViewsTest;

public class ImageLoader
{
    private BackgroundTaskQueue _taskQueue = new BackgroundTaskQueue();

    public void Load(string source, Action<ImageHandle> onLoad)
    {
        _taskQueue.Enqueue(() =>
        {
            if (source.StartsWith("http"))
            {
                using var client = new HttpClient();
                using var resp = client.Send(new HttpRequestMessage(HttpMethod.Get,source));
                resp.EnsureSuccessStatusCode();
                using var image = HostImage.Create(resp.Content.ReadAsStream());
                image.CreateTexture(out var imageHandle).Wait();
                IApplication.Get().MainDispatcher.Enqueue(() =>
                {
                    onLoad(imageHandle);
                });
            }
            else
            {
                using var client = new HttpClient();
                using var resp = client.Send(new HttpRequestMessage(HttpMethod.Get,source));
                resp.EnsureSuccessStatusCode();
                using var data = File.OpenRead(source);
                using var image = HostImage.Create(data);
                image.CreateTexture(out var imageHandle).Wait();
                IApplication.Get().MainDispatcher.Enqueue(() =>
                {
                    onLoad(imageHandle);
                });
            }
        });
    }
}