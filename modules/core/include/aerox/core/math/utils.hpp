#pragma once

template<typename T>
T abs(T a);

template <typename T>
T abs(T a)
{
    if(a < 0.0f)
    {
        return -a;
    }

    return a;
}
