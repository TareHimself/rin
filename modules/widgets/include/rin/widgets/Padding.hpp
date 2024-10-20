#pragma once

struct Padding
{
    float left;
    float right;
    float top;
    float bottom;

    explicit Padding(float inAll);
    Padding(float inHorizontal, float inVertical);
    Padding(float inLeft, float inRight, float inTop, float inBottom);
};
