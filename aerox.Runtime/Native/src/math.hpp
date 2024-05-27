#pragma once
#include <glm/glm.hpp>

#include "macro.hpp"

struct Vector2
{
    float x = 0.0f;
    float y = 0.0f;
    operator glm::vec2();
    Vector2() = default;
    Vector2(glm::vec2 other);
};

struct Vector3
{
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;
    operator glm::vec3();
    Vector3() = default;
    Vector3(glm::vec3 other);
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
};

struct Matrix3
{
    Vector3 column1;
    Vector3 column2;
    Vector3 column3;
    operator glm::mat3();
    Matrix3() = default;
    Matrix3(glm::mat3 other);
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
};

struct Transform
{
    Vector3 Location;
    Quaternion Rotation;
    Vector3 Scale;
};


EXPORT void mathMultiplyQuatQuat(Quaternion * result,Quaternion * left,Quaternion * right);

EXPORT void mathMultiplyQuatVector4(Vector4 * result,Quaternion * left,Vector4 * b);

EXPORT void mathMultiplyMatrix4Matrix4(Matrix4 * result,Matrix4 * left,Matrix4 * right);

EXPORT void mathMultiplyMatrix4Vector4(Vector4 * result,Matrix4 * left,Vector4 * right);

EXPORT void mathInverseMatrix4(Matrix4 * result,Matrix4 * a);

EXPORT void mathQuatToMatrix4(Matrix4 * result,Quaternion * a);

EXPORT void mathTranslateMatrix4(Matrix4 * result,Matrix4 * target,Vector3 * translation);

EXPORT void mathQuatFromAngle(Quaternion * result,float angle,Vector3 * axis);

EXPORT void mathRotateMatrix4(Matrix4* result,Matrix4 * target, float angle, Vector3* axis);

EXPORT void mathScaleMatrix4(Matrix4 * result,Matrix4 * target,Vector3 * scale);

EXPORT void mathMatrix4ToTransform(Transform * result,Matrix4 * target);

EXPORT void mathTransformToMatrix4(Matrix4 * result,Transform * target);

EXPORT void mathGlmOrthographic(Matrix4 * result,float left,float right,float bottom, float top);


EXPORT void mathRotateMatrix3(Matrix3* result,Matrix3 * target, float angle);

EXPORT void mathScaleMatrix3(Matrix3 * result,Matrix3 * target,Vector2 * scale);

EXPORT void mathTranslateMatrix3(Matrix3 * result,Matrix3 * target,Vector2 * translation);

EXPORT void mathInverseMatrix3(Matrix3 * result,Matrix3 * a);

EXPORT void mathMultiplyMatrix3Matrix3(Matrix3 * result,Matrix3 * left,Matrix3 * right);

EXPORT void mathMultiplyMatrix3Vector3(Vector3 * result,Matrix3 * left,Vector3 * right);




