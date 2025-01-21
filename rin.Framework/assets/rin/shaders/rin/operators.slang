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
