using System.Runtime.CompilerServices;

namespace Rin.Engine.Core.Extensions;

public static class BufferExtensions
{
    public static Buffer<T> ToBuffer<T>(this T[] array) where T : unmanaged
    {
        unsafe
        {
            fixed (T* data = array)
            {
                return new Buffer<T>(data,array.Length);
            }
        }
    }
    
    public static Buffer<T> ToBuffer<T>(this IEnumerable<T> items) where T : unmanaged
    {
        unsafe
        {
            var asArray = items.ToArray();
            fixed (T* data = asArray)
            {
                return new Buffer<T>(data,asArray.Length);
            }
        }
    }
}