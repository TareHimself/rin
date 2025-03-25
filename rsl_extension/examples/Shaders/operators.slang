
float4 operator *(float4x4 matrix, float4 vec)
{
    return mul(matrix, vec);
}
float4 operator *(float4 vec, float4x4 matrix)
{
    return mul(vec, matrix);
}
float4x4 operator *(float4x4 matrixLeft, float4x4 matrixRight)
{
    return mul(matrixLeft, matrixRight);
}

float3 operator *(float3x3 matrix, float3 vec)
{
    return mul(matrix, vec);
}
float3 operator *(float3 vec, float3x3 matrix)
{
    return mul(vec, matrix);
}
float3x3 operator *(float3x3 matrixLeft, float3x3 matrixRight)
{
    return mul(matrixLeft, matrixRight);
}

// [OverloadRank(15)]
// [ForceInline]
// public matrix<float, N, N> operator *<let N : int>(matrix<float, N, N> m1, matrix<float, N, N> m2)
// {
//     return mul(m2, m1);
// }

// [OverloadRank(15)]
// [ForceInline]
// public matrix<half, N, N> operator *<let N : int>(matrix<half, N, N> m1, matrix<half, N, N> m2)
// {
//     return mul(m2, m1);
// }

// [OverloadRank(15)]
// [ForceInline]
// public matrix<double, N, N> operator *<let N : int>(matrix<double, N, N> m1, matrix<double, N, N> m2)
// {
//     return mul(m2, m1);
// }

// [ForceInline]
// [OverloadRank(15)]
// public matrix<T, R, L> operator *<T : __BuiltinFloatingPointType, let L : int, let C : int, let R : int>(matrix<T, C, L> m1, matrix<T, R, C> m2)
// {
//     return mul(m2, m1);
// }

// [ForceInline]
// [OverloadRank(15)]
// public vector<T, R> operator *<T : __BuiltinFloatingPointType, let C : int, let R : int>(vector<T, C> v, matrix<T, R, C> m)
// {
//     return mul(m, v);
// }

// [ForceInline]
// [OverloadRank(15)]
// public vector<T, C> operator *<T : __BuiltinFloatingPointType, let C : int, let R : int>(matrix<T, R, C> m, vector<T, R> v)
// {
//     return mul(v, m);
// }