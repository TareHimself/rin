#include "WText.hpp"
#include "rin/widgets/WidgetsModule.hpp"
#include "rin/widgets/graphics/SimpleBatchedDrawCommand.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"

WText::WText(const Shared<USDFContainer>& inSDF)
{
    sdf = inSDF;
}

void WText::CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands)
{
    const std::string text{"Hello World"};
    const auto item = sdf->Get("=");
    const auto scale = 10.0f;
    auto size = Vec2{item.width, item.height}.Cast<float>() * scale;
    drawCommands.Add(SimpleBatchedDrawCommand::Builder().AddMtsdf(
        item.atlas, size, transform.transform, Vec4{0.0f}, Vec4{1.0f},item.uv).Finish());
}

Vec2<float> WText::ComputeContentSize()
{
    return Vec2{0.0f};
}
