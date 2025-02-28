#pragma once
#include <fstream>
#include <glm/glm.hpp>

#include "macro.hpp"

struct Vector2
{
    float x = 0.0f;
    float y = 0.0f;
    operator glm::vec2();
    Vector2() = default;
    Vector2(glm::vec2 other);

    friend std::ostream& operator <<(std::ostream& out, const Vector2& data)
    {
        out << "[Vector2]" << "\n" << "[" << data.x << "," << data.y << "]";
        return out;
    }
};

struct Vector3
{
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;
    operator glm::vec3();
    Vector3() = default;
    Vector3(glm::vec3 other);

    friend std::ostream& operator <<(std::ostream& out, const Vector3& data)
    {
        out << "[Vector3]" << "\n"  << "[" << data.x << "," << data.y  << "," << data.z << "]";
        return out;
    }
};

struct Vector4
{
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;
    float w = 0.0f;
    operator glm::vec4();

    Vector4() = default;
    Vector4(glm::vec4 other);

    friend std::ostream& operator <<(std::ostream& out, const Vector4& data)
    {
        out << "[Vector4]" << "\n" << "[" << data.x << "," << data.y  << "," << data.z << "," << data.w << "]";
        return out;
    }
};

struct Quaternion
{
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;
    float w = 0.0f;
    operator glm::quat();

    Quaternion() = default;
    Quaternion(glm::quat other);

    friend std::ostream& operator <<(std::ostream& out, const Quaternion& data)
    {
        out << "[Quaternion]" << "\n" << "[" << data.x << "," << data.y  << "," << data.z << "," << data.w << "]";
        return out;
    }
};

struct Rotator
{
    float pitch = 0.0f;
    float yaw = 0.0f;
    float roll = 0.0f;
    operator glm::vec3();
    Rotator() = default;
    Rotator(glm::vec3 other);

    friend std::ostream& operator <<(std::ostream& out, const Rotator& data)
    {
        out << "[Vector3]" << "\n"  << "[" << data.pitch << "," << data.yaw  << "," << data.roll << "]";
        return out;
    }
};

struct Matrix3
{
    Vector3 column1;
    Vector3 column2;
    Vector3 column3;
    operator glm::mat3();
    Matrix3() = default;
    Matrix3(glm::mat3 other);

    friend std::ostream& operator <<(std::ostream& out, const Matrix3& data)
    {
        out << "[Matrix3]" << "\n" << data.column1 << "\n" << data.column2 << "\n" << data.column3;
        return out;
    }
};

struct Matrix4
{
    Vector4 column1;
    Vector4 column2;
    Vector4 column3;
    Vector4 column4;
    operator glm::mat4();
    Matrix4() = default;
    Matrix4(glm::mat4 other);

    friend std::ostream& operator <<(std::ostream& out, const Matrix4& data)
    {
        out << "[Matrix4]" << "\n" << data.column1 << "\n" << data.column2 << "\n" << data.column3 << "\n" << data.column4;
        return out;
    }
};

struct Transform
{
    Vector3 Location;
    Quaternion Rotation;
    Vector3 Scale;
    friend std::ostream& operator <<(std::ostream& out, const Transform& data)
    {
        out << "[Transform]" << "\n" << data.Location << "\n" << data.Rotation << "\n" << data.Scale;
        return out;
    }
};


EXPORT void mathMultiplyQuatQuat(Quaternion * result,Quaternion * left,Quaternion * right);

EXPORT void mathMultiplyQuatVector4(Vector4 * result,Quaternion * left,Vector4 * b);

EXPORT void mathMultiplyMatrix4Matrix4(Matrix4 * result,Matrix4 * left,Matrix4 * right);

EXPORT void mathMultiplyMatrix4Vector4(Vector4 * result,Matrix4 * left,Vector4 * right);

EXPORT void mathInverseMatrix4(Matrix4 * result,Matrix4 * a);

EXPORT void mathQuatToMatrix4(Matrix4 * result,Quaternion * a);

EXPORT void mathTranslateMatrix4(Matrix4 * result,Matrix4 * target,Vector3 * translation);

EXPORT void mathQuatToRotator(Rotator * result,Quaternion * quat);

EXPORT void mathQuatFromAngle(Quaternion * result,float angle,Vector3 * axis);

EXPORT void mathQuatLookAt(Quaternion * result,Vector3* from,Vector3* to, Vector3* up);

EXPORT void mathRotateMatrix4(Matrix4* result,Matrix4 * target, float angle, Vector3* axis);

EXPORT void mathScaleMatrix4(Matrix4 * result,Matrix4 * target,Vector3 * scale);

EXPORT void mathMatrix4ToTransform(Transform * result,Matrix4 * target);

EXPORT void mathTransformToMatrix4(Matrix4 * result,Transform * target);

EXPORT void mathGlmOrthographic(Matrix4 * result,float left,float right,float bottom, float top);

EXPORT void mathGlmPerspective(Matrix4 * result,float fov,float aspect,float near,float far);

EXPORT void mathRotateMatrix3(Matrix3* result,Matrix3 * target, float angle);

EXPORT void mathScaleMatrix3(Matrix3 * result,Matrix3 * target,Vector2 * scale);

EXPORT void mathTranslateMatrix3(Matrix3 * result,Matrix3 * target,Vector2 * translation);

EXPORT void mathInverseMatrix3(Matrix3 * result,Matrix3 * a);

EXPORT void mathMultiplyMatrix3Matrix3(Matrix3 * result,Matrix3 * left,Matrix3 * right);

EXPORT void mathMultiplyMatrix3Vector3(Vector3 * result,Matrix3 * left,Vector3 * right);




