using System.Numerics;

namespace rin.Core.Math;

public interface IVector<out TVector, out TValue> : 
    ICloneable<TVector>,
    IEnumerable<TValue>
{
    
}