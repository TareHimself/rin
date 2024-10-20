#pragma once
struct Quat
{
    float x;
    float y;
    float z;
    float w;

    explicit Quat();
    explicit Quat(float inX, float inY, float inZ, float inW);
};
