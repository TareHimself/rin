#include "msdf.hpp"

GlyphContext::GlyphContext()
{
    contour = nullptr;
}

GlyphContext::~GlyphContext()
{
    contour = nullptr;
}

Vec2::operator msdfgen::Vector2()
{
    return msdfgen::Vector2(x,y);
}

_declspec(dllexport) GlyphContext * msdfNewContext()
{
    return new GlyphContext();
}

_declspec(dllexport) void msdfFreeContext(GlyphContext * context)
{
    delete context;
}

_declspec(dllexport) void msdfGlyphContextMoveTo(GlyphContext * context, Vec2* to)
{
    context->hasContent = true;
    if(!(context->contour && context->contour->edges.empty()))
    {
        context->contour = &context->shape.addContour();
    }

    context->position = *to;
}

_declspec(dllexport) void msdfGlyphContextLineTo(GlyphContext * context, Vec2* to)
{
    context->hasContent = true;
    msdfgen::Point2 endpoint = *to;
    if(endpoint != context->position)
    {
        context->contour->addEdge(msdfgen::EdgeHolder(context->position,endpoint));
        context->position = endpoint;
    }
}

_declspec(dllexport) void msdfGlyphContextQuadraticBezierTo(GlyphContext * context, Vec2* control, Vec2* to)
{
    context->hasContent = true;
    msdfgen::Point2 endpoint = *to;
    if (endpoint != context->position) {
        context->contour->addEdge(
            msdfgen::EdgeHolder(context->position, *control, endpoint));
        context->position = endpoint;
    }
}

_declspec(dllexport) void msdfGlyphContextCubicBezierTo(GlyphContext * context, Vec2* control1, Vec2* control2, Vec2* to)
{
    context->hasContent = true;
    msdfgen::Point2 endpoint = *to;
    const auto cross = msdfgen::crossProduct(*control1 - endpoint,
                                             *control2 - endpoint);
    if (endpoint != context->position || cross) {
        context->contour->addEdge(msdfgen::EdgeHolder(
            context->position, *control1, *control2,
            endpoint));
        context->position = endpoint;
    }
}

_declspec(dllexport) void msdfEndContext(GlyphContext * context)
{
    
    if (!context->shape.contours.empty() && context->shape.contours.back().edges.empty())
        context->shape.contours.pop_back();

    context->shape.normalize();
}

_declspec(dllexport) void msdfGenerateMSDF(GlyphContext* context, int padding,float angleThreshold,float pixelRange, RenderMsdfCallback callback)
{

    if(!context->hasContent) {    
        return;
    }
    
    msdfgen::edgeColoringByDistance(context->shape,angleThreshold);

    auto shape = context->shape;
    
    const auto bounds = shape.getBounds();

    auto width = bounds.r - bounds.l;
    auto height = bounds.t - bounds.b;
    auto offsetX = bounds.l;
    auto offsetY = height - bounds.t;
    
    auto bmpWidth = static_cast<int>(std::ceil(width)) + padding * 2;
    auto bmpHeight = static_cast<int>(std::ceil(height)) + padding * 2;

    msdfgen::Bitmap<float, 3> bmp(bmpWidth,bmpHeight);

    auto transform = msdfgen::SDFTransformation{{{1.0,1.0},{(offsetX * -1.0) + padding,offsetY + padding}},{}};
    msdfgen::generateMSDF(bmp,shape,transform,pixelRange);
    

    std::vector<unsigned char> data;
    data.reserve(3 * bmpWidth * bmpHeight);

    for(auto y = 0; y < bmpHeight; y++) {
        for(auto x = 0; x < bmpWidth; x++) {
            const auto pixel = bmp(x,bmpHeight - (y + 1));
            data.push_back(msdfgen::pixelFloatToByte(pixel[0]));
            data.push_back(msdfgen::pixelFloatToByte(pixel[1]));
            data.push_back(msdfgen::pixelFloatToByte(pixel[2]));
        }
    }

    callback(data.data(),bmpWidth,bmpHeight,data.size());
}

_declspec(dllexport) void msdfGenerateMTSDF(GlyphContext* context,int padding,float angleThreshold,float pixelRange, RenderMsdfCallback callback)
{
    if(!context->hasContent) {    
        return;
    }

    msdfgen::edgeColoringByDistance(context->shape,angleThreshold);

    auto shape = context->shape;
    
    const auto bounds = shape.getBounds();

    auto width = bounds.r - bounds.l;
    auto height = bounds.t - bounds.b;
    auto offsetX = bounds.l;
    auto offsetY = height - bounds.t;
    
    auto bmpWidth = static_cast<int>(std::ceil(width)) + padding * 2;
    auto bmpHeight = static_cast<int>(std::ceil(height)) + padding * 2;

    msdfgen::Bitmap<float, 4> bmp(bmpWidth,bmpHeight);

    auto transform = msdfgen::SDFTransformation{{{1.0,1.0},{(offsetX * -1.0) + padding,offsetY + padding}},{}};
    msdfgen::generateMTSDF(bmp,shape,transform,pixelRange);
    

    std::vector<unsigned char> data;
    data.reserve(4 * bmpWidth * bmpHeight);

    for(auto y = 0; y < bmpHeight; y++) {
        for(auto x = 0; x < bmpWidth; x++) {
            const auto pixel = bmp(x,bmpHeight - (y + 1));
            data.push_back(msdfgen::pixelFloatToByte(pixel[0]));
            data.push_back(msdfgen::pixelFloatToByte(pixel[1]));
            data.push_back(msdfgen::pixelFloatToByte(pixel[2]));
            data.push_back(msdfgen::pixelFloatToByte(pixel[3]));
        }
    }

    callback(data.data(),bmpWidth,bmpHeight,data.size());
}
