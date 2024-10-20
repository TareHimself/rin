#include "rin/widgets/SDFContainer.hpp"
#include "rin/widgets/USDFContainer.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/graphics/ResourceManager.hpp"


SDFContainer::SDFContainer() = default;

bool SDFContainer::Has(const std::string& id) const
{
    return _items.contains(id);
}

SDFItem SDFContainer::Get(const std::string& id)
{
    return _items.at(id);
}

void SDFContainer::AddAtlas(const std::shared_ptr<Image<unsigned char>>& atlas)
{
    _atlases.push_back(atlas);
}

void SDFContainer::AddItem(const SDFItem& item)
{
    _items.emplace(item.id, item);
}

Shared<USDFContainer> SDFContainer::Upload()
{
    std::vector<int> atlases{};

    auto resourceManager = GraphicsModule::Get()->GetResourceManager();

    for (auto& atlas : _atlases)
    {
        atlases.push_back(resourceManager->CreateTexture(*atlas, ImageFormat::RGBA8, vk::Filter::eLinear));
    }

    std::unordered_map<std::string, USDFItem> items{};

    for (auto& [id,item] : _items)
    {
        const auto& image = _atlases.at(item.atlas);
        const auto atlasSize = image->GetSize().Cast<float>();
        const auto rect = Vec4{item.x, item.y, item.x + item.width, item.y + item.height}.Cast<float>();
        const Vec4<float> uv{rect.x / atlasSize.x, rect.y / atlasSize.y, rect.z / atlasSize.x, rect.w / atlasSize.y};
        items.emplace(id, USDFItem{item.id, item.x, item.y, item.width, item.height, atlases.at(item.atlas), uv});
    }

    return newShared<USDFContainer>(items, atlases);
}
