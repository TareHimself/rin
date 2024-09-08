#pragma once
#include "Vec4.hpp"
namespace aerox
{
    template<typename T>
    struct Matrix4
    {
        Vec4<T> column1;
        Vec4<T> column2;
        Vec4<T> column3;
        Vec4<T> column4;

        explicit Matrix4(const T& fill);
        explicit Matrix4(const glm::mat<4,4,T>& mat);
    };

    template <typename T>
    Matrix4<T>::Matrix4(const T& fill) : column1(Vec4{fill}) , column2(Vec4{fill}), column3(Vec4{fill}), column4(Vec4{fill})
    {

    }

    template <typename T>
    Matrix4<T>::Matrix4(const glm::mat<4, 4, T>& mat) : column1(Vec4{mat[0]}) , column2(Vec4{mat[1]}), column3(Vec4{mat[2]}), column4(Vec4{mat[3]})
    {

    }
}
