#pragma once
#include <functional>

template <typename T>
T abs(T a);

template <typename T>
T interpolate(const T& begin, const T& end, const T& alpha);

template <typename T>
T interpolate(const T& begin, const T& end, const T& alpha, const std::function<T(T)>& method);

template <typename T>
T abs(T a)
{
    if (a < 0.0f)
    {
        return -a;
    }

    return a;
}

template <typename T>
T interpolate(const T& begin, const T& end, const T& alpha)
{
    return begin + (end - begin) * alpha;
}

template <typename T>
T interpolate(const T& begin, const T& end, const T& alpha, const std::function<T(T)>& method)
{
    return begin + (end - begin) * method(alpha);
}
