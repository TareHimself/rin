#include "rin/widgets/USDFContainer.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/graphics/ResourceManager.hpp"

USDFContainer::USDFContainer()
{
    
}

USDFContainer::USDFContainer(const std::unordered_map<std::string, USDFItem>& items, const std::vector<int>& atlases)
{
    _items = items;
    _atlases = atlases;
}

bool USDFContainer::Has(const std::string& id) const
{
    return _items.contains(id);
}

USDFItem USDFContainer::Get(const std::string& id)
{
    return _items.at(id);
}

void USDFContainer::OnDispose(bool manual)
{
    Disposable::OnDispose(manual);
    GraphicsModule::Get()->GetResourceManager()->FreeTextures(_atlases);
}
