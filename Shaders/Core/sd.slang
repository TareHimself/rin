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

float sdCircle(float2 location,float2 center, float r)
{
    // return max(0.0, distance(location, center));
    return length(location - center) - r;
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

// // https://www.shadertoy.com/view/fsdyzB
// float sdBox(float2 center, float2 size)
// {
//     radius.xy = (center.x > 0.0) ? radius.xy : radius.zw;
//     radius.x = (center.y > 0.0) ? radius.x : radius.y;

//     float2 q = abs(center) - size + radius.x;
//     return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius.x;
// }

// https://www.shadertoy.com/view/fsdyzB
float sdRoundedBox(float2 p, float2 halfSize, float4 radius)
{
    radius.xy = (p.x > 0.0) ? radius.xy : radius.zw;
    radius.x = (p.y > 0.0) ? radius.x : radius.y;

    float2 q = abs(p) - halfSize + radius.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius.x;
}

float sdRoundBox(in float2 p, in float2 b, in float4 r)
{
    r.xy = (p.x > 0.0) ? r.xy : r.zw;
    r.x = (p.y > 0.0) ? r.x : r.y;
    var q = abs(p) - b + r.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
}

float sdRoundedBox(in float2 p, in float2 center, in float2 halfSize, in float4 r)
{
    // Shift point into box-local coordinates
    p -= center;

    // Select the appropriate corner radius based on quadrant
    r.xy = (p.x > 0.0) ? r.xy : r.zw;
    r.x = (p.y > 0.0) ? r.x : r.y;

    // Compute distance to box
    float2 q = abs(p) - halfSize + r.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
}


static int num_segments = 5;
static int num_iterations = 3;
static int num_start_params = 3;

// factor should be positive
// it decreases the step size when lowered.
// Lowering the factor and increasing iterations increases the area in which
// the iteration converges, but this is quite costly
static float factor = 1.;

static float eps = .005;

float newton_iteration3(float3 coeffs, float x) {
    float a2 = coeffs[2] + x;
    float a1 = coeffs[1] + x * a2;

    float f = coeffs[0] + x * a1;
    float f1 = ((x + a2) * x) + a1;

    return x - f / f1;
}

float halley_iteration3(float3 coeffs, float x) {
    float a2 = coeffs[2] + x;
    float a1 = coeffs[1] + x * a2;

    float f = coeffs[0] + x * a1;

    float b2 = a2 + x;

    float f1 = a1 + x * b2;
    float f2 = 2. * (b2 + x);

    return x - (2. * f * f1) / (2. * f1 * f1 - f * f2);
}

// From Trisomie21
// But instead of his cancellation fix i'm using a newton iteration
int solve_cubic(float a, float b, float c, out float3 r) {
    float p = b - a * a / 3.0;
    float q = a * (2.0 * a * a - 9.0 * b) / 27.0 + c;
    float p3 = p * p * p;
    float d = q * q + 4.0 * p3 / 27.0;
    float offset = -a / 3.0;
    if (d >= 0.0) { // Single solution
        float z = sqrt(d);
        float u = (-q + z) / 2.0;
        float v = (-q - z) / 2.0;
        u = sign(u) * pow(abs(u), 1.0 / 3.0);
        v = sign(v) * pow(abs(v), 1.0 / 3.0);
        r[0] = offset + u + v;

        // Single newton iteration to account for cancellation
        float f = ((r[0] + a) * r[0] + b) * r[0] + c;
        float f1 = (3. * r[0] + 2. * a) * r[0] + b;

        r[0] -= f / f1;

        return 1;
    }
    float u = sqrt(-p / 3.0);
    float v = acos(-sqrt(-27.0 / p3) * q / 2.0) / 3.0;
    float m = cos(v), n = sin(v) * 1.732050808;

    // Single newton iteration to account for cancellation
    //(once for every root)
    r[0] = offset + u * (m + m);
    r[1] = offset - u * (n + m);
    r[2] = offset + u * (n - m);

    float3 f = ((r + a) * r + b) * r + c;
    float3 f1 = (3. * r + 2. * a) * r + b;

    r -= f / f1;

    return 3;
}

// Sign computation is pretty straightforward:
// I'm solving a cubic equation to get the intersection count
// of a ray from the current point to infinity and parallel to the x axis
// Also i'm computing the intersection count with the tangent in the end points of the curve
float cubic_bezier_sign(float2 uv, float2 p0, float2 p1, float2 p2, float2 p3) {

    float cu = (-p0.y + 3. * p1.y - 3. * p2.y + p3.y);
    float qu = (3. * p0.y - 6. * p1.y + 3. * p2.y);
    float li = (-3. * p0.y + 3. * p1.y);
    float co = p0.y - uv.y;

    float3 roots;
    int n_roots = solve_cubic(qu / cu, li / cu, co / cu, roots);

    int n_ints = 0;

    for (int i = 0; i < 3; i++) {
        if (i < n_roots) {
            if (roots[i] >= 0. && roots[i] <= 1.) {
                float x_pos = ((((-p0.x + 3. * p1.x - 3. * p2.x + p3.x) * roots[i] + (3. * p0.x - 6. * p1.x + 3. * p2.x)) * roots[i]) + (-3. * p0.x + 3. * p1.x)) * roots[i] + p0.x;
                if (x_pos < uv.x) {
                    n_ints++;
                }
            }
        }
    }

    float2 tang1 = p0.xy - p1.xy;
    float2 tang2 = p2.xy - p3.xy;

    float2 nor1 = float2(tang1.y, -tang1.x);
    float2 nor2 = float2(tang2.y, -tang2.x);

    if (p0.y < p1.y) {
        if ((uv.y <= p0.y) && (dot(uv - p0.xy, nor1) < 0.)) {
            n_ints++;
        }
    }
    else {
        if (!(uv.y <= p0.y) && !(dot(uv - p0.xy, nor1) < 0.)) {
            n_ints++;
        }
    }

    if (p2.y < p3.y) {
        if (!(uv.y <= p3.y) && dot(uv - p3.xy, nor2) < 0.) {
            n_ints++;
        }
    }
    else {
        if ((uv.y <= p3.y) && !(dot(uv - p3.xy, nor2) < 0.)) {
            n_ints++;
        }
    }

    if (n_ints == 0 || n_ints == 2 || n_ints == 4) {
        return 1.;
    }
    else {
        return -1.;
    }
}

// cubic bezier distance by segmentation based on the one by iq
// see https://www.shadertoy.com/view/XdVBWd

float length2(float2 v) { return dot(v, v); }

float segment_dis_sq(float2 p, float2 a, float2 b) {
    float2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length2(pa - ba * h);
}

float cubic_bezier_segments_dis_sq(float2 uv, float2 p0, float2 p1, float2 p2, float2 p3) {
    float d0 = 1e38;
    float2 a = p0;
    for (int i = 1; i < num_segments; i++)
    {
        float t = float(i) / float(num_segments - 1);
        float s = 1.0 - t;
        float2 b = p0 * s * s * s + p1 * 3.0 * s * s * t + p2 * 3.0 * s * t * t + p3 * t * t * t;
        d0 = min(d0, segment_dis_sq(uv, a, b));
        a = b;
    }

    return d0;
}

float cubic_bezier_segments_dis(float2 uv, float2 p0, float2 p1, float2 p2, float2 p3) {
    return sqrt(cubic_bezier_segments_dis_sq(uv, p0, p1, p2, p3));
}

float cubic_bezier_normal_iteration(float t, float2 a0, float2 a1, float2 a2, float2 a3) {
    // horner's method
    float2 a_2 = a2 + t * a3;
    float2 a_1 = a1 + t * a_2;
    float2 b_2 = a_2 + t * a3;

    float2 uv_to_p = a0 + t * a_1;
    float2 tang = a_1 + t * b_2;

    float l_tang = dot(tang, tang);
    return t - factor * dot(tang, uv_to_p) / l_tang;
}

float cubic_bezier_normal_iteration2(float t, float2 a0, float2 a1, float2 a2, float2 a3) {
    // horner's method
    float2 a_2 = a2 + t * a3;
    float2 a_1 = a1 + t * a_2;
    float2 b_2 = a_2 + t * a3;

    float2 uv_to_p = a0 + t * a_1;
    float2 tang = a_1 + t * b_2;
    float2 snd_drv = 2. * (b_2 + t * a3);

    float l_tang = dot(tang, tang);

    float fac = dot(tang, snd_drv) / (2. * l_tang);
    float d = -dot(tang, uv_to_p);

    float t2 = d / (l_tang + fac * d);

    return t + factor * t2;
}

float cubic_bezier_normal_iteration3(float t, float2 a0, float2 a1, float2 a2, float2 a3) {
    float2 tang = (3. * a3 * t + 2. * a2) * t + a1;
    float3 poly = float3(dot(a0, tang), dot(a1, tang), dot(a2, tang)) / dot(a3, tang);

    /* newton iteration on this polynomial is equivalent to cubic_bezier_normal_iteration */
    return newton_iteration3(poly, t);

    /* halley iteration on this polynomial is equivalent to cubic_bezier_normal_iteration2 */
    // return halley_iteration3(poly,t);
}

float cubic_bezier_dis_approx_sq(float2 uv, float2 p0, float2 p1, float2 p2, float2 p3) {
    float2 a3 = (-p0 + 3. * p1 - 3. * p2 + p3);
    float2 a2 = (3. * p0 - 6. * p1 + 3. * p2);
    float2 a1 = (-3. * p0 + 3. * p1);
    float2 a0 = p0 - uv;

    float d0 = 1e38;

    float t0 = 0.;
    float t;

    for (int i = 0; i < num_start_params; i++) {
        t = t0;
        for (int j = 0; j < num_iterations; j++) {
#ifdef SECOND_ORDER
            t = cubic_bezier_normal_iteration2(t, a0, a1, a2, a3);
#else
            t = cubic_bezier_normal_iteration(t, a0, a1, a2, a3);
#endif
        }
        t = clamp(t, 0., 1.);
        float2 uv_to_p = ((a3 * t + a2) * t + a1) * t + a0;
        d0 = min(d0, dot(uv_to_p, uv_to_p));

        t0 += 1. / float(num_start_params - 1);
    }

    return d0;
}

float sdCubic(float2 position, float2 begin, float2 end, float2 controlA, float2 controlB) {
    return sqrt(cubic_bezier_dis_approx_sq(position, begin,controlA,controlB, end));
}
