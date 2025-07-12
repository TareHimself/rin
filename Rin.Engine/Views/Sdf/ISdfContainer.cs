using Rin.Engine.Graphics;

namespace Rin.Engine.Views.Sdf;

public interface ISdfContainer
{
    public string[] GetImages();
    public string[] GetVectors();
    public IHostImage? LoadImage(string id);
    public SdfImage? GetImage(string id);
    public SdfVector? GetVector(string id);
    public bool HasImage(string id);
    public bool HasVector(string id);
    public string AddImage(IHostImage image);
    public void AddVector(SdfVector vector);
    public string[] AddImages(IEnumerable<HostImage> images);
    public void AddVectors(IEnumerable<SdfVector> vectors);
}