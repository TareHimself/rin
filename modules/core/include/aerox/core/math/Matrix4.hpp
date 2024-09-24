#pragma once
#include <glm/gtx/transform.hpp>

#include "Vec3.hpp"
#include "Vec4.hpp"
template<typename T>
    struct Matrix4
    {
    private:
        glm::mat<4,4,T> _mat;
    public:
        explicit Matrix4(float inData);
        explicit Matrix4(const glm::mat<4,4,T>& inMat);
        operator glm::mat<4,4,T>() const;

        Matrix4 Translate(const Vec3<T>& translation) const;

        Matrix4 Rotate(float radi) const;

        Matrix4 RotateDeg(float deg) const;

        Matrix4 Scale(const Vec3<T>& scale) const;

        Matrix4 operator*(const Matrix4& other) const;

        Vec3<T> operator*(const Vec3<T>& other) const;
    };

    template <typename T>
    Matrix4<T>::Matrix4(float inData)
    {
        _mat = glm::mat4(inData);
    }

    template <typename T>
    Matrix4<T>::Matrix4(const glm::mat<4, 4, T>& inMat)
    {
        _mat = inMat;
    }

    template <typename T>
    Matrix4<T>::operator glm::mat<4, 4, T>() const
    {
        return _mat;
    }

    template <typename T>
    Matrix4<T> Matrix4<T>::Translate(const Vec3<T>& translation) const
    {
        return static_cast<Matrix4>(glm::translate(static_cast<glm::mat<4,4,T>>(*this),static_cast<glm::vec<3,T>>(translation)));
    }

    template <typename T>
    Matrix4<T> Matrix4<T>::Rotate(float radi) const
    {
        return static_cast<Matrix4>(glm::rotate(static_cast<glm::mat<4,4,T>>(*this),radi));
    }

    template <typename T>
    Matrix4<T> Matrix4<T>::RotateDeg(float deg) const
    {
        return static_cast<Matrix4>(glm::rotate(static_cast<glm::mat<4,4,T>>(*this),glm::radians(deg)));
    }

    template <typename T>
    Matrix4<T> Matrix4<T>::Scale(const Vec3<T>& scale) const
    {
        return static_cast<Matrix4>(glm::scale(static_cast<glm::mat<4,4,T>>(*this),static_cast<glm::vec<3,T>>(scale)));
    }

    template <typename T>
    Matrix4<T> Matrix4<T>::operator*(const Matrix4& other) const
    {
        return static_cast<Matrix4>(static_cast<glm::mat<4,4,T>>(*this) * static_cast<glm::mat<4,4,T>>(other));
    }

    template <typename T>
    Vec3<T> Matrix4<T>::operator*(const Vec3<T>& other) const
    {
        return static_cast<Matrix4>(static_cast<glm::mat<4,4,T>>(*this) * static_cast<glm::vec<3,T>>(other));
    }