#include "sdf.hpp"

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

EXPORT_IMPL GlyphContext * sdfContextNew()
{
    return new GlyphContext();
}

EXPORT_IMPL void sdfContextFree(GlyphContext * context)
{
    delete context;
}

EXPORT_IMPL void sdfContextMoveTo(GlyphContext * context, Vec2* to)
{
    context->hasContent = true;
    if(!(context->contour && context->contour->edges.empty()))
    {
        context->contour = &context->shape.addContour();
    }

    context->position = *to;
}

EXPORT_IMPL void sdfContextLineTo(GlyphContext * context, Vec2* to)
{
    context->hasContent = true;
    msdfgen::Point2 endpoint = *to;
    if(endpoint != context->position)
    {
        context->contour->addEdge(msdfgen::EdgeHolder(context->position,endpoint));
        context->position = endpoint;
    }
}

EXPORT_IMPL void sdfContextQuadraticBezierTo(GlyphContext * context, Vec2* control, Vec2* to)
{
    context->hasContent = true;
    msdfgen::Point2 endpoint = *to;
    if (endpoint != context->position) {
        context->contour->addEdge(
            msdfgen::EdgeHolder(context->position, *control, endpoint));
        context->position = endpoint;
    }
}

EXPORT_IMPL void sdfContextCubicBezierTo(GlyphContext * context, Vec2* control1, Vec2* control2, Vec2* to)
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

EXPORT_IMPL void sdfContextEnd(GlyphContext * context)
{
    
    if (!context->shape.contours.empty() && context->shape.contours.back().edges.empty())
        context->shape.contours.pop_back();

    context->shape.normalize();
}

EXPORT_IMPL void sdfContextGenerateMSDF(GlyphContext* context,float angleThreshold,float pixelRange, GenerateCallback callback)
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
    
    auto bmpWidth = static_cast<int>(std::ceil(width));
    auto bmpHeight = static_cast<int>(std::ceil(height));

    msdfgen::Bitmap<float, 3> bmp(bmpWidth,bmpHeight);

    auto transform = msdfgen::SDFTransformation{{{1.0,1.0},{(offsetX),offsetY}},{}};
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

    callback(data.data(),bmpWidth,bmpHeight,data.size(),width,height);
}

EXPORT_IMPL void sdfContextGenerateMTSDF(GlyphContext* context,float angleThreshold,float pixelRange, GenerateCallback callback)
{
    if(!context->hasContent) {    
        return;
    }

    msdfgen::edgeColoringByDistance(context->shape,angleThreshold);

    auto shape = context->shape;
    
    const auto bounds = shape.getBounds();

    // auto width = (bounds.r - bounds.l) + 1.0f;
    // auto height = (bounds.t - bounds.b) + 1.0f;
    // auto offsetX = (width - bounds.r) - 0.5f;
    // auto offsetY = (height - bounds.t) - 0.5f;
    auto width = (bounds.r - bounds.l);
    auto height = (bounds.t - bounds.b);
    auto offsetX = (width - bounds.r);
    auto offsetY = (height - bounds.t);
    width += (pixelRange * 2.0f);
    height += (pixelRange * 2.0f);
    offsetX += pixelRange;
    offsetY += pixelRange;
    auto bmpWidth = static_cast<int>(std::ceil(width));
    auto bmpHeight = static_cast<int>(std::ceil(height));

    msdfgen::Bitmap<float, 4> bmp(bmpWidth,bmpHeight);

    auto transform = msdfgen::SDFTransformation{{{1.0,1.0},{offsetX,offsetY}},{}};
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

    callback(data.data(),bmpWidth,bmpHeight,data.size(),width,height);
}

msdfgen::SDFTransformation computeTransformation(msdfgen::Shape& shape, float pixelRange)
{
    msdfgen::Vector2 scale = msdfgen::Vector2(1.0f);
    msdfgen::Vector2 translate(0.0f);
    msdfgen::Range pxRange(pixelRange);
    msdfgen::Range range(pxRange/ std::min(scale.x, scale.y));
    double avgScale = .5*(scale.x+scale.y);
    msdfgen::Shape::Bounds bounds = shape.getBounds();
    double l = bounds.l, b = bounds.b, r = bounds.r, t = bounds.t;
    auto width = static_cast<int>(std::ceil(r - l));
    auto height = static_cast<int>(std::ceil(t - b));
        

    // if (outputDistanceShift) {
    //     Range &rangeRef = rangeMode == RANGE_PX ? pxRange : range;
    //     double rangeShift = -outputDistanceShift*(rangeRef.upper-rangeRef.lower);
    //     rangeRef.lower += rangeShift;
    //     rangeRef.upper += rangeShift;
    // }

    // Auto-frame
    
    msdfgen::Vector2 frame(width, height);
    frame += 2*pxRange.lower;
    // if (l >= r || b >= t)
    //     l = 0, b = 0, r = 1, t = 1;
    // if (frame.x <= 0 || frame.y <= 0)
    //     ABORT("Cannot fit the specified pixel range.");
    msdfgen::Vector2 dims(r-l, t-b);
    
    if (dims.x*frame.y < dims.y*frame.x) {
        translate.set(.5*(frame.x/frame.y*dims.y-dims.x)-l, -b);
        scale = avgScale = frame.y/dims.y;
    } else {
        translate.set(-l, .5*(frame.y/frame.x*dims.x-dims.y)-b);
        scale = avgScale = frame.x/dims.x;
    }
    translate -= pxRange.lower/scale;
    
    range = pxRange/ std::min(scale.x, scale.y);
    
    // Compute output
    const msdfgen::SDFTransformation transformation(msdfgen::Projection(scale, translate), range);
    return transformation;
}
