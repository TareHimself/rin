#pragma once

#include "Vec2.h"
#include "Vec3.h"

namespace rin
{
    template <typename T = float>
    struct Mat3
    {
    private:
        glm::mat<3, 3,T, glm::defaultp> _mat;
    public:
        template<std::enable_if_t<std::is_default_constructible_v<T>>* = nullptr>
        Mat3()
        {
            _mat = {};
        }
        Mat3(const T& data)
        {
            _mat = {data};
        }

        Mat3(const glm::mat<3, 3,T, glm::defaultp>& data)
        {
            _mat = data;
        }
        
        Vec3<T> operator*(const Vec3<T>& vec) const
        {
            auto r = _mat * static_cast<glm::vec<3,T>>(Vec3<T>{vec.x, vec.y, vec.z});
            return Vec3<T>(r.x,r.y,r.z);
        }
        
        Vec2<T> operator*(const Vec2<T>& vec) const
        {
            auto r = *this * Vec3<T>{vec.x,vec.y,1};
            return Vec2<T>(r.x,r.y);
        }
        Mat3 Translate(const Vec2<T>& translation) const
        {
            return Mat3{glm::translate(_mat, static_cast<glm::vec<2,T>>(translation))};
        }
        Mat3 Scale(const Vec2<T>& scale) const
        {
            return Mat3{glm::scale(_mat, static_cast<glm::vec<2,T>>(scale))};
        }
        Mat3 Rotate(const float& angle) const
        {
            return Mat3{glm::rotate(_mat, angle)};
        }
    };
}
