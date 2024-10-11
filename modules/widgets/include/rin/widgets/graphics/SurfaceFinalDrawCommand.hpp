#pragma once
#include <cstdint>
#include <optional>

#include "WidgetCustomDrawCommand.hpp"
#include "rin/widgets/graphics/QuadInfo.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"

struct SurfaceFinalDrawCommand
{
    enum class Type
    {
        None,
        ClipDraw,
        ClipClear,
        BatchedDraw,
        CustomDraw
    };

    std::optional<uint64_t> size{};
    std::optional<ClipInfo> clipInfo{};
    std::optional<std::vector<QuadInfo>> quads{};
    std::optional<uint32_t> mask{};
    Shared<WidgetCustomDrawCommand> custom{};
    Type type = Type::None;

    void SetQuads(const std::vector<QuadInfo>& inQuads, const uint32_t inMask)
    {
        type = Type::BatchedDraw;
        size = inQuads.size() * sizeof(QuadInfo);
        quads = inQuads;
        mask = inMask;
    }
};
