﻿using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Views;
using Rin.Engine.Views.Animation;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace rin.Examples.Common.Views;

public class AsyncWebImage : CoverImage
{
    public event Action<bool>? OnLoaded;
    
    public AsyncWebImage(string uri) : base()
    {
        LoadFile(uri).ConfigureAwait(false);
    }

    private async Task LoadFile(string uri)
    {
        try
        {
            using var client = new HttpClient();
            var stream = await client.GetStreamAsync(uri);
            using var img = await Image.LoadAsync<Rgba32>(stream);
            using var imgData = img.ToBuffer();
            await SGraphicsModule.Get().GetTextureManager().CreateTexture(imgData,
                new Extent3D
                {
                    Width = (uint)img.Width,
                    Height = (uint)img.Height
                },
                ImageFormat.RGBA8).Then(c => TextureId = c);
            OnLoaded?.Invoke(true);
        }
        catch (Exception e)
        {
            OnLoaded?.Invoke(false);
        }
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        base.CollectContent(transform, commands);
        commands.AddRect(transform, GetContentSize(), Color.Black.Clone(a: 0.5f),BorderRadius);
    }
}