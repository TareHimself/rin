using System.Numerics;

namespace rin.Framework.Core.Math;

public interface IVec<out TVector, out TValue> : 
    ICloneable<TVector>,
    IEnumerable<TValue>
{
    
}