using Rin.Framework;
using Rin.Framework.Graphics;
using Rin.Framework.Views.Content;

namespace rin.Examples.ViewsTest;

public class NetworkImageView : ImageView
{
    private readonly string _url;
    private readonly ImageLoader _loader = SFramework.Provider.Get<ImageLoader>();
    public NetworkImageView(string url)
    {
        _url = url;
        _loader.Load(_url, (imageHandle) =>
        {
            ImageHandle = imageHandle;
        });
    }
}