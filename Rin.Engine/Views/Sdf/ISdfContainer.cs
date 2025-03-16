namespace Rin.Engine.Views.Sdf;

public interface ISdfContainer
{
    public SdfVector? GetVector(string id);
    public SdfResult? GetResult(int id);
    public IEnumerable<SdfVector> GetVectors();
}