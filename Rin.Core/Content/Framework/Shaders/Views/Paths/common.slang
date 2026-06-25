struct Path {
    float4x4 transform;
    float2 color;
}

enum SegmentType {
    Line,
    Arc,
    Bezier,
    Quadratic
}

struct Segment {
    SegmentType type;
    float2 p0;
    float2 p1;
    float2 p2;
    float2 p3;
}

struct Fragment {
    
}