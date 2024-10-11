#include "rin/widgets/WidgetsModule.hpp"
#include "rin/core/GRuntime.hpp"
#include "rin/core/utils.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/graphics/WindowRenderer.hpp"
#include "rin/widgets/WidgetWindowSurface.hpp"
#include <msdfgen/msdfgen.h>
#include <rpack/rpack.hpp>

WidgetsModule* WidgetsModule::Get()
{
    return GRuntime::Get()->GetModule<WidgetsModule>();
}

std::string WidgetsModule::GetName()
{
    return "Widgets Module";
}

void WidgetsModule::Startup(GRuntime* runtime)
{
    _rendererCreatedHandle = _graphicsModule->onRendererCreated->Add(this, &WidgetsModule::OnRendererCreated);
    _rendererDestroyedHandle = _graphicsModule->onRendererDestroyed->Add(this, &WidgetsModule::OnRendererDestroyed);
    _library = InitFreetype();
}

void WidgetsModule::Shutdown(GRuntime* runtime)
{
    _rendererCreatedHandle.UnBind();
    _rendererDestroyedHandle.UnBind();
    _batchShader.reset();
    _stencilShader.reset();
}

bool WidgetsModule::IsDependentOn(RinModule* module)
{
    return instanceOf<GraphicsModule>(module);
}

void WidgetsModule::RegisterRequiredModules()
{
    RinModule::RegisterRequiredModules();
    _graphicsModule = GRuntime::Get()->RegisterModule<GraphicsModule>();
}

void WidgetsModule::OnRendererCreated(WindowRenderer* renderer)
{
    if (!_batchShader)
    {
        _batchShader = GraphicsShader::FromFile(getResourcesPath() / "shaders" / "widgets" / "batch.rsl");
        _stencilShader = GraphicsShader::FromFile(getResourcesPath() / "shaders" / "widgets" / "stencil_single.rsl");
    }
    auto surf = newShared<WidgetWindowSurface>(renderer);
    surf->Init();
    _windowSurfaces.emplace(renderer, surf);
}

void WidgetsModule::OnRendererDestroyed(WindowRenderer* renderer)
{
    _windowSurfaces[renderer]->Dispose();
    _windowSurfaces.erase(renderer);
}

Shared<WidgetWindowSurface> WidgetsModule::GetSurface(Window* window) const
{
    if (const auto renderer = _graphicsModule->GetRenderer(window))
    {
        if (_windowSurfaces.contains(renderer))
        {
            return _windowSurfaces.at(renderer);
        }
    }

    return {};
}

Shared<WidgetWindowSurface> WidgetsModule::GetSurface(const Shared<Window>& window) const
{
    return GetSurface(window.get());
}

Shared<GraphicsShader> WidgetsModule::GetBatchShader() const
{
    return _batchShader;
}

Shared<GraphicsShader> WidgetsModule::GetStencilShader() const
{
    return _stencilShader;
}

std::shared_ptr<FT_Library> WidgetsModule::InitFreetype()
{
    const auto library = new FT_Library;

    if (FT_Init_FreeType(library))
    {
        delete library;
        return {};
    }

    auto ftLibrary = std::shared_ptr<FT_Library>(library, [](FT_Library* ptr)
    {
        FT_Done_FreeType(*ptr);
        delete ptr;
    });

    return ftLibrary;
}

#define F26DOT6_TO_DOUBLE(x) (1/64.*double(x))
#define F16DOT16_TO_DOUBLE(x) (1/65536.*double(x))
#define DOUBLE_TO_F16DOT16(x) FT_Fixed(65536.*x)

msdfgen::Point2 ftPoint2(const FT_Vector& vector)
{
    return msdfgen::Point2{
        F26DOT6_TO_DOUBLE(vector.x), F26DOT6_TO_DOUBLE(vector.y)
    };
}

struct FtContext
{
    msdfgen::Point2 position{};
    msdfgen::Shape* shape = nullptr;
    msdfgen::Contour* contour = nullptr;
    bool hasContent = false;
};

std::shared_ptr<Image<unsigned char>> WidgetsModule::MtsdfFromGlyph(int code, const FT_Face& face,
                                                                    const float pixelRange, const float angleThreshold)
{
    const auto glyph_index = FT_Get_Char_Index(face, code);
    if (FT_Load_Glyph(
        face, /* handle to face object         */
        glyph_index,
        FT_LOAD_DEFAULT))
    {
        return nullptr;
    }

    msdfgen::Shape shape{};


    shape.contours.clear();
    shape.inverseYAxis = false;
    FtContext context{};
    context.shape = &shape;

    FT_Outline_Funcs ftFunctions;

    ftFunctions.move_to = [](const FT_Vector* to, void* user)
    {
        const auto context = static_cast<FtContext*>(user);
        if (!(context->contour && context->contour->edges.empty()))
            context->contour = &context->shape->addContour();
        context->position = ftPoint2(*to);
        return 0;
    };

    ftFunctions.line_to = [](const FT_Vector* to, void* user)
    {
        const auto context = static_cast<FtContext*>(user);
        const auto endpoint = ftPoint2(*to);
        if (endpoint != context->position)
        {
            context->contour->addEdge(
                msdfgen::EdgeHolder(context->position, endpoint));
            context->position = endpoint;
        }
        context->hasContent = true;
        return 0;
    };

    ftFunctions.conic_to = [](const FT_Vector* control, const FT_Vector* to,
                              void* user)
    {
        const auto context = static_cast<FtContext*>(user);
        const auto endpoint = ftPoint2(*to);
        if (endpoint != context->position)
        {
            context->contour->addEdge(
                msdfgen::EdgeHolder(context->position, ftPoint2(*control), endpoint));
            context->position = endpoint;
        }
        context->hasContent = true;
        return 0;
    };

    ftFunctions.cubic_to = [](const FT_Vector* control1,
                              const FT_Vector* control2, const FT_Vector* to,
                              void* user)
    {
        const auto context = static_cast<FtContext*>(user);
        const auto endpoint = ftPoint2(*to);
        const auto cross = crossProduct(ftPoint2(*control1) - endpoint,
                                        ftPoint2(*control2) - endpoint);
        if (endpoint != context->position || cross)
        {
            context->contour->addEdge(msdfgen::EdgeHolder(
                context->position, ftPoint2(*control1), ftPoint2(*control2),
                endpoint));
            context->position = endpoint;
        }
        context->hasContent = true;
        return 0;
    };

    ftFunctions.shift = 0;
    ftFunctions.delta = 0;

    if (const FT_Error error = FT_Outline_Decompose(&face->glyph->outline, &ftFunctions, &context))
    {
        return {};
    }

    if (!context.hasContent) return {};

    if (!shape.contours.empty() && shape.contours.back().edges.empty())
    {
        shape.contours.pop_back();
    }

    shape.normalize();

    edgeColoringByDistance(shape, angleThreshold);

    const auto bounds = shape.getBounds();

    auto width = bounds.r - bounds.l;
    auto height = bounds.t - bounds.b;
    auto offsetX = bounds.l;
    auto offsetY = height - bounds.t;

    auto bmpWidth = static_cast<int>(std::ceil(width));
    auto bmpHeight = static_cast<int>(std::ceil(height));

    auto result = std::make_shared<Image<unsigned char>>(bmpWidth, bmpHeight, 4);
    msdfgen::Bitmap<float, 4> bmp{bmpWidth, bmpHeight};
    //msdfgen::Bitmap<float, 4> bmp(bmpWidth,bmpHeight);

    auto transform = msdfgen::SDFTransformation{{{1.0, 1.0}, {(offsetX * -1.0), offsetY}}, {}};
    generateMTSDF(bmp, shape, transform, pixelRange);

    for (auto y = 0; y < bmpHeight; y++)
    {
        for (auto x = 0; x < bmpWidth; x++)
        {
            auto bmpPixel = bmp(x, y);
            result->At(x, y, 0) = msdfgen::pixelFloatToByte(bmpPixel[0]);
            result->At(x, y, 1) = msdfgen::pixelFloatToByte(bmpPixel[1]);
            result->At(x, y, 2) = msdfgen::pixelFloatToByte(bmpPixel[2]);
            result->At(x, y, 3) = msdfgen::pixelFloatToByte(bmpPixel[3]);
        }
    }
    return result;
}

bool WidgetsModule::GenerateAtlases(SDFContainer& result, const FT_Face& face, int pixelSize, int atlasSize,
                                    int padding,
                                    float pixelRange, float angleThreshold)
{
    if (FT_Set_Pixel_Sizes(face, 0, pixelSize))
    {
        return false;
    }

    std::unordered_map<int, std::shared_ptr<Image<unsigned char>>> generated{};

    for (auto i = 32; i < 127; i++)
    {
        if (auto result = MtsdfFromGlyph(i, face, pixelRange, angleThreshold); result && !result->GetElementCount() ==
            0)
        {
            generated.emplace(i, result);
        }
    }

    using namespace rpack;
    std::vector<Packer> packers{};
    std::unordered_map<int, std::pair<int, int>> mapping{};

    for (auto it = generated.begin(); it != generated.end(); ++it)
    {
        if (packers.empty())
        {
            packers.emplace_back(atlasSize, atlasSize, padding);
        }

        auto packerId = static_cast<int>(packers.size()) - 1;

        auto& [id,image] = *it;

        auto insertion = packers.at(packerId).Pack(image->GetWidth(), image->GetHeight());

        if (!insertion.has_value())
        {
            packers.emplace_back(atlasSize, atlasSize, 10);
            --it;
        }
        else
        {
            int insertionIdx = insertion.value();
            mapping.emplace(id, std::pair(packerId, insertionIdx));
        }
    }


    std::vector<std::shared_ptr<Image<unsigned char>>> atlases{};
    atlases.reserve(packers.size());

    for (auto& packer : packers)
    {
        atlases.push_back(std::make_shared<Image<unsigned char>>(atlasSize, atlasSize, 4));
    }

    for (auto& [id,info] : mapping)
    {
        auto atlasId = info.first;
        auto img = generated.at(id);
        auto pos = packers.at(info.first).GetRects().at(info.second);
        img->CopyTo(*atlases.at(atlasId), Vec2{0}, img->GetSize(), Vec2{pos.x, pos.y});
    }
}
