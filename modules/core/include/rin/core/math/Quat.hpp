#pragma once
struct Quat
{
public:
    float x;
    float y;
    float z;
    float w;

public:
    explicit Quat();
    explicit Quat(float inX,float inY,float inZ,float inW);
};