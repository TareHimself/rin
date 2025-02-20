#pragma once
#include "Quat.h"
#include "Vec2.h"
#include "Vec3.h"
#include "Vec4.h"

namespace rin
{
    template <typename T = float>
    struct Mat4
    {
    private:
        glm::mat<4, 4,T, glm::defaultp> _mat;
    public:
        template<std::enable_if_t<std::is_default_constructible_v<T>>* = nullptr>
        Mat4()
        {
            _mat = {};
        }

        Mat4(const T& data)
        {
            _mat = {data};
        }

        Vec4<T> operator*(const Vec4<T>& vec) const
        {
            return static_cast<Vec4<T>>(_mat * static_cast<glm::vec<4,T>>(vec));
        }
        
        

        Mat4 Translate(const Vec3<T>& translation)
        {
            return Mat4{glm::translate(_mat, static_cast<glm::vec<3,T>>(translation))};
        }
        Mat4 Scale(const Vec3<T>& scale)
        {
            return Mat4{glm::scale(_mat, static_cast<glm::vec<3,T>>(scale))};
        }
        Mat4 Rotate(const Quat& rotation)
        {
            return Mat4{_mat * static_cast<glm::quat>(rotation)};
        }
    };
}
