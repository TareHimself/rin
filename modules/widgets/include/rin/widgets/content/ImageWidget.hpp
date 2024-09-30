#pragma once
#include "rin/core/math/Vec4.hpp"
#include "rin/widgets/Widget.hpp"

class ImageWidget : public Widget
{
    int _textureId = -1;
    Vec4<float> _borderRadius{0.0f};
    Vec4<float> _tint{1.0f};
public:
    void SetTextureId(const int& textureId);
    int GetTextureId() const;
    void SetBorderRadius(const Vec4<float>& borderRadius);
    Vec4<float> GetBorderRadius() const;
    void SetTint(const Vec4<float>& tint);
    Vec4<float> GetTint() const;
    void Collect(const TransformInfo& transform, WidgetDrawCommands& drawCommands) override;

protected:
    Vec2<float> ComputeContentSize() override;
};
