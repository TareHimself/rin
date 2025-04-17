// https://iquilezles.org/articles/distfunctions2d/
#pragma once
float dot2(float2 v) { return dot(v, v); }

float sdSegment(in float2 p, in float2 a, in float2 b)
{
    float2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float sdBezier(in float2 pos, in float2 A, in float2 B, in float2 control )
{    
    float2 a = control - A;
    float2 control = A - 2.0*control + B;
    float2 B = a * 2.0;
    float2 d = A - pos;
    float kk = 1.0/dot(control,control);
    float kx = kk * dot(a,control);
    float ky = kk * (2.0*dot(a,a)+dot(d,control)) / 3.0;
    float kz = kk * dot(d,a);      
    float res = 0.0;
    float p = ky - kx*kx;
    float p3 = p*p*p;
    float q = kx*(2.0*kx*kx-3.0*ky) + kz;
    float h = q*q + 4.0*p3;
    if( h >= 0.0) 
    { 
        h = sqrt(h);
        float2 x = (float2(h,-h)-q)/2.0;
        float2 uv = sign(x)*pow(abs(x), float2(1.0/3.0));
        float t = clamp( uv.x+uv.y-kx, 0.0, 1.0 );
        res = dot2(d + (B + control*t)*t);
    }
    else
    {
        float z = sqrt(-p);
        float v = acos( q/(p*z*2.0) ) / 3.0;
        float m = cos(v);
        float n = sin(v)*1.732050808;
        float3  t = clamp(float3(m+m,-n-m,n-m)*z-kx,0.0,1.0);
        res = min( dot2(d+(B+control*t.x)*t.x),
                   dot2(d+(B+control*t.y)*t.y) );
        // the third root cannot be the closest
        // res = min(res,dot2(d+(B+control*t.z)*t.z));
    }
    return sqrt( res );
}

float sdCircle(float2 p, float r)
{
    return length(p) - r;
}

float sdBezierCubic(float2 p, float2 a, float2 b, float2 controlA, float2 controlB)
{
    // Coefficients of the cubic Bezier
    float2 a_coeff = b - 3.0 * controlB + 3.0 * controlA - a;
    float2 b_coeff = 3.0 * controlB - 6.0 * controlA + 3.0 * a;
    float2 c_coeff = 3.0 * controlA - 3.0 * a;
    float2 d_coeff = a;

    // Implicit function coefficients for finding closest point on curve
    float A = dot(a_coeff, a_coeff);
    float B = dot(3.0 * a_coeff, b_coeff);
    float C = dot(3.0 * a_coeff, c_coeff) + dot(b_coeff, b_coeff);
    float D = dot(3.0 * a_coeff, d_coeff) + dot(2.0 * b_coeff, c_coeff);
    float E = dot(b_coeff, d_coeff) + dot(c_coeff, c_coeff);
    float F = dot(c_coeff, d_coeff);

    // Solve for the parameter t (find closest point on curve)
    float t = 0.0;

    // Subdivide and Conquer for t
    float t_low = 0.0;
    float t_high = 1.0;

    for (int i = 0; i < 5; i++) // Iterate a few times for reasonable accuracy
    {
        t = (t_low + t_high) * 0.5;

        float2 pos = a_coeff * t * t * t + b_coeff * t * t + c_coeff * t + d_coeff;
        float2 derivative = 3.0 * a_coeff * t * t + 2.0 * b_coeff * t + c_coeff;

        float2 toPoint = p - pos;
        float dist = length(toPoint);

        if (dist < 1e-6) return 0.0; // Close enough

        float dotProd = dot(toPoint, derivative);

        if (dotProd < 0.0)
            t_high = t;
        else
            t_low = t;
    }

    // Evaluate at the closest point
    float2 closestPoint = a_coeff * t * t * t + b_coeff * t * t + c_coeff * t + d_coeff;

    // Calculate the signed distance
    float dist = length(p - closestPoint);

    // Determine the sign (inside/outside) - winding number or normal check
    float2 tangent = normalize(3.0 * a_coeff * t * t + 2.0 * b_coeff * t + c_coeff); // Tangent at closest point
    float2 normal = float2(-tangent.y, tangent.x);                                   // 90-degree rotation for normal

    float sign = sign(dot(p - closestPoint, normal)); // Inside if dot > 0

    return dist * sign;
}