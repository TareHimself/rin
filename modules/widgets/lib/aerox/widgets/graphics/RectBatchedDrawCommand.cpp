#include "aerox/widgets/graphics/RectBatchedDrawCommand.hpp"

namespace aerox::widgets
{
    RectBatchedDrawCommand::RectBatchedDrawCommand(const std::vector<Rect>& rects)
    {
        _rects = rects;
    }

    std::vector<QuadInfo> RectBatchedDrawCommand::ComputeQuads() const
    {
        std::vector<QuadInfo> quads{};
        quads.reserve(_rects.size());
        for (auto &rect : _rects)
        {
            quads.emplace_back(-1,-1,rect.color,rect.borderRadius,rect.size,rect.transform);
        } 
        return quads;
    }

    Shared<RectBatchedDrawCommand> RectBatchedDrawCommand::New(const std::vector<Rect>& rects)
    {
        return newShared<RectBatchedDrawCommand>(rects);
    }
}
