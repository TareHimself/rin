﻿using rin.Framework.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace rin.Framework.Views.Sdf;

public class SdfContainer : Disposable
{
    private readonly Image<Rgba32>[] _atlases;
    private readonly Dictionary<string, SdfVector> _vectors;

    public SdfContainer(Dictionary<string, SdfVector> vectors, IEnumerable<Image<Rgba32>> atlases)
    {
        _vectors = vectors;
        _atlases = atlases.ToArray();
    }


    public SdfVector? GetVector(string id)
    {
        return _vectors.GetValueOrDefault(id);
    }

    protected override void OnDispose(bool isManual)
    {
        foreach (var atlas in _atlases) atlas.Dispose();
    }
}