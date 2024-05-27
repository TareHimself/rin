#include "msdf.hpp"

#include <iostream>

GlyphContext::GlyphContext()
{
    shape = new msdfgen::Shape;
    contour = nullptr;
}

GlyphContext::~GlyphContext()
{
    contour = nullptr;
    delete shape;
}

GlyphContext * msdfNewContext()
{
    return new GlyphContext();
}

void msdfFreeContext(GlyphContext * context)
{
    delete static_cast<GlyphContext*>(context);
}

void msdfGlyphContextMoveTo(GlyphContext * context, Vec2* to)
{
    auto ctx = static_cast<GlyphContext*>(context);
    if(!(ctx->contour && ctx->contour->edges.empty()))
    {
        ctx->contour = &ctx->shape->addContour();
    }

    ctx->position = *to;
}

void msdfGlyphContextLineTo(GlyphContext * context, Vec2* to)
{
    auto ctx = static_cast<GlyphContext*>(context);
    msdfgen::Point2 endpoint = *to;
    if(endpoint != ctx->position)
    {
        ctx->contour->addEdge(msdfgen::EdgeHolder(ctx->position,endpoint));
        ctx->position = endpoint;
    }
}

void msdfGlyphContextQuadraticBezierTo(GlyphContext * context, Vec2* control, Vec2* to)
{
    auto ctx = static_cast<GlyphContext*>(context);
    msdfgen::Point2 endpoint = *to;
    if (endpoint != ctx->position) {
        ctx->contour->addEdge(
            msdfgen::EdgeHolder(ctx->position, *control, endpoint));
        ctx->position = endpoint;
    }
}

void msdfGlyphContextCubicBezierTo(GlyphContext * context, Vec2* control1, Vec2* control2, Vec2* to)
{
    auto ctx = static_cast<GlyphContext*>(context);
    msdfgen::Point2 endpoint = *to;
    const auto cross = msdfgen::crossProduct(*control1 - endpoint,
                                             *control2 - endpoint);
    if (endpoint != ctx->position || cross) {
        ctx->contour->addEdge(msdfgen::EdgeHolder(
            ctx->position, *control1, *control2,
            endpoint));
        ctx->position = endpoint;
    }
}

void msdfEndContext(GlyphContext * context)
{
    auto ctx = static_cast<GlyphContext*>(context);
    if (!ctx->shape->contours.empty() && ctx->shape->contours.back().edges.empty())
        ctx->shape->contours.pop_back();

    ctx->shape->normalize();

    msdfgen::edgeColoringByDistance(*ctx->shape,3.0);
}

void msdfRenderContext(GlyphContext * context,RenderMsdfCallback callback)
{
    auto ctx = static_cast<GlyphContext*>(context);

    auto shape = *ctx->shape;
    
    if(shape.edgeCount() == 0 && shape.contours.empty()) {
        return;
    }
    const auto bounds = shape.getBounds();
    auto width = bounds.r - bounds.l;
    auto height = bounds.t - bounds.b;
    auto offsetX = bounds.l;
    auto offsetY = height - bounds.t;
    
    int padding = 3;
    auto bmpWidth = static_cast<int>(std::ceil(width)) + padding * 2;
    auto bmpHeight = static_cast<int>(std::ceil(height)) + padding * 2;
    
    msdfgen::Bitmap<float, 3> msdf(bmpWidth,bmpHeight);
    const auto projection = msdfgen::Projection{{1.0,1.0},{(offsetX * -1.0) + padding,offsetY + padding}};
    generateMSDF(msdf,shape,projection,30.0);

    std::vector<unsigned char> data;

    const auto totalPixels = bmpWidth * bmpHeight;

    data.reserve(4 * totalPixels);

    for(auto y = 0; y < bmpHeight; y++) {
        for(auto x = 0; x < bmpWidth; x++) {
            const auto pixel = msdf(x,bmpHeight - (y + 1));

            // Artificially Make border empty
            if(x == 0 || y == 0 || x == bmpWidth - 1 || y == bmpHeight - 1)
            {
                data.push_back(255);
                data.push_back(255);
                data.push_back(255);
                data.push_back(255);
                continue;
            }

            data.push_back(msdfgen::pixelFloatToByte(pixel[0]));
            data.push_back(msdfgen::pixelFloatToByte(pixel[1]));
            data.push_back(msdfgen::pixelFloatToByte(pixel[2]));
            data.push_back(255);
        }
    }

    callback(data.data(),bmpWidth,bmpHeight,data.size());
}
