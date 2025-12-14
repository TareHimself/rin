#pragma once
#include "macro.hpp"
#include <msdfgen.h>
#include <cstdint>
#include <cstddef>
struct GlyphContext
{
    msdfgen::Point2 position{};
    msdfgen::Shape shape{};
    msdfgen::Contour * contour;
    bool hasContent = false;
    GlyphContext();
    ~GlyphContext();
};

struct Vec2
{
    float x;
    float y;

    operator msdfgen::Vector2();
};

RIN_NATIVE_API GlyphContext * sdfContextNew();

RIN_NATIVE_API void sdfContextFree(GlyphContext * context);

RIN_NATIVE_API void sdfContextBeginContour(GlyphContext * context);

RIN_NATIVE_API void sdfContextEndContour(GlyphContext * context);

RIN_NATIVE_API void sdfContextMoveTo(GlyphContext * context, Vec2* to);

RIN_NATIVE_API void sdfContextLineTo(GlyphContext * context, Vec2* to);

RIN_NATIVE_API void sdfContextQuadraticBezierTo(GlyphContext * context, Vec2* control,Vec2* to);

RIN_NATIVE_API void sdfContextCubicBezierTo(GlyphContext * context, Vec2* control1,Vec2* control2,Vec2* to);

RIN_NATIVE_API void sdfContextFinish(GlyphContext * context);

using GenerateCallback = void(RIN_CALLBACK_CONVENTION *)(void * data,uint32_t pixelWidth,uint32_t pixelHeight,uint32_t count,double width,double height);
RIN_NATIVE_API void sdfContextGenerateMSDF(GlyphContext * context,float angleThreshold,float pixelRange,GenerateCallback callback);

RIN_NATIVE_API void sdfContextGenerateMTSDF(GlyphContext * context,float angleThreshold,float pixelRange,GenerateCallback callback);

msdfgen::SDFTransformation computeTransformation(msdfgen::Shape& shape,float pixelRange);


