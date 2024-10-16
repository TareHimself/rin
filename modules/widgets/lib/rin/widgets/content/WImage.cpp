#include "rin/widgets/content/WImage.hpp"
#include "rin/graphics/DeviceImage.hpp"
#include "rin/graphics/ResourceManager.hpp"
#include "rin/widgets/graphics/SimpleBatchedDrawCommand.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"

void WImage::SetTextureId(const int& textureId)
{
    auto isNew = _textureId != textureId;
    _textureId = textureId;
    if (isNew)
    {
        CheckSize();
    }
}

int WImage::GetTextureId() const
{
    return _textureId;
}

void WImage::SetBorderRadius(const Vec4<float>& borderRadius)
{
    _borderRadius = borderRadius;
}

Vec4<float> WImage::GetBorderRadius() const
{
    return _borderRadius;
}

void WImage::SetTint(const Vec4<float>& tint)
{
    _tint = tint;
}

Vec4<float> WImage::GetTint() const
{
    return _tint;
}

void WImage::CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands)
{
    if (_textureId < 0)
    {
        drawCommands.Add(
            SimpleBatchedDrawCommand::Builder{}
            .AddRect(
                GetContentSize(),
                transform.transform,
                GetBorderRadius(),
                GetTint()
            )
            .Finish()
        );
    }
    else
    {
        drawCommands.Add(
            SimpleBatchedDrawCommand::Builder{}
            .AddTexture(
                _textureId,
                GetContentSize(),
                transform.transform,
                GetBorderRadius(),
                GetTint()
            )
            .Finish()
        );
    }
}

Vec2<float> WImage::ComputeContentSize()
{
    if (auto vkImage = GraphicsModule::Get()->GetResourceManager()->GetTextureImage(GetTextureId()))
    {
        auto extent = vkImage->GetExtent();
        return {static_cast<float>(extent.width), static_cast<float>(extent.height)};
    }
    return {0.0f, 0.0f};
}
