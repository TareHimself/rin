#pragma once
#include "Vec2.hpp"
#include <glm/gtx/matrix_transform_2d.hpp>
namespace aerox
{

    template<typename T>
    struct Matrix3
    {
    private:
        glm::mat<3,3,T> _mat;
    public:
        explicit Matrix3(float inData);
        explicit Matrix3(const glm::mat<3,3,T>& inMat);
        operator glm::mat<3,3,T>() const;

        Matrix3 Translate(const Vec2<T>& translation) const;

        Matrix3 Rotate(float radi) const;

        Matrix3 RotateDeg(float deg) const;

        Matrix3 Scale(const Vec2<T>& scale) const;

        Matrix3 operator*(const Matrix3& other) const;

        Vec2<T> operator*(const Vec2<T>& other) const;
    };

    template <typename T>
    Matrix3<T>::Matrix3(float inData)
    {
        _mat = glm::mat<3,3,T>(inData);
    }

    template <typename T>
   Matrix3<T>::Matrix3(const glm::mat<3,3,T>& inMat)
    {
        _mat = inMat;
    }

    template <typename T>
    Matrix3<T>::operator glm::mat<3,3,T> () const
    {
        return _mat;
    }

    template <typename T>
    Matrix3<T> Matrix3<T>::Translate(const Vec2<T>& translation) const
    {
        return static_cast<Matrix3<T>>(glm::translate(static_cast<glm::mat<3,3,T>>(*this),static_cast<glm::vec<2,T>>(translation)));
    }

    template <typename T>
    Matrix3<T> Matrix3<T>::Rotate(float radi) const
    {
        return static_cast<Matrix3<T>>(glm::rotate(static_cast<glm::mat<3,3,T>>(*this),radi));
    }

    template <typename T>
    Matrix3<T> Matrix3<T>::RotateDeg(float deg) const
    {
        return static_cast<Matrix3<T>>(glm::rotate(static_cast<glm::mat<3,3,T>>(*this),glm::radians(deg)));
    }

    template <typename T>
    Matrix3<T> Matrix3<T>::Scale(const Vec2<T>& scale) const
    {
        return static_cast<Matrix3<T>>(glm::scale(static_cast<glm::mat<3,3,T>>(*this),static_cast<glm::vec<2,T>>(scale)));
    }

    template <typename T>
    Matrix3<T> Matrix3<T>::operator*(const Matrix3& other) const
    {
        return Matrix3{_mat * other._mat};
    }

    template <typename T>
    Vec2<T> Matrix3<T>::operator*(const Vec2<T>& other) const
    {
        auto r = _mat * glm::vec<3,float>(other.x,other.y,1.0);
        return Vec2{r.x,r.y};
    }
}
