// namespace Rin.Engine.Views.Sdf;
//
// public class SdfContainer : ISdfContainer
// {
//     private readonly Dictionary<int, SdfResult> _atlases;
//     private readonly Dictionary<string, SdfVector> _vectors;
//
//     public SdfContainer(Dictionary<string, SdfVector> vectors, Dictionary<int, SdfResult> atlases)
//     {
//         _vectors = vectors;
//         _atlases = atlases;
//     }
//
//
//     public SdfVector? GetVector(string id)
//     {
//         return _vectors.GetValueOrDefault(id);
//     }
//
//     public SdfResult? GetResult(int id)
//     {
//         return _atlases.GetValueOrDefault(id);
//     }
//
//     public IEnumerable<SdfVector> GetVectors()
//     {
//         return _vectors.Values;
//     }
//
//     public Dictionary<int, SdfResult> GetAtlases()
//     {
//         return _atlases;
//     }
// }

