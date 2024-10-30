float3 rgb2lin(float3 rgb){
    return rgb;
    return pow(rgb,float3(2.2));
}

float3 lin2rgb(float3 lin){
    return lin;
    return pow(lin,float3(1.0 / 2.2));
}

