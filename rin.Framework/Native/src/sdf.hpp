#pragma once
#include "macro.hpp"
#include <msdfgen.h>

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

EXPORT_DECL GlyphContext * sdfContextNew();

EXPORT_DECL void sdfContextFree(GlyphContext * context);

EXPORT_DECL void sdfContextMoveTo(GlyphContext * context, Vec2* to);

EXPORT_DECL void sdfContextLineTo(GlyphContext * context, Vec2* to);

EXPORT_DECL void sdfContextQuadraticBezierTo(GlyphContext * context, Vec2* control,Vec2* to);

EXPORT_DECL void sdfContextCubicBezierTo(GlyphContext * context, Vec2* control1,Vec2* control2,Vec2* to);

EXPORT_DECL void sdfContextEnd(GlyphContext * context);

using GenerateCallback = void(__stdcall *)(void * data,uint32_t pixelWidth,uint32_t pixelHeight,uint32_t count,double width,double height);
EXPORT_DECL void sdfContextGenerateMSDF(GlyphContext * context,float angleThreshold,float pixelRange,GenerateCallback callback);

EXPORT_DECL void sdfContextGenerateMTSDF(GlyphContext * context,float angleThreshold,float pixelRange,GenerateCallback callback);

msdfgen::SDFTransformation computeTransformation(msdfgen::Shape& shape,float pixelRange);


